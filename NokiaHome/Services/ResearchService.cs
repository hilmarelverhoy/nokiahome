using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Research;
using NokiaHome.Settings;

namespace NokiaHome.Services;

/// <summary>
/// Implements the research pipeline:
///   1. Transcribe audio via Whisper (optional).
///   2. Ask GPT to plan up to 5 web requests that will help answer the question.
///   3. Fetch each URL (plain HTTP GET, extract visible text from HTML).
///   4. Ask GPT to summarise each page in context of the question.
///   5. Ask GPT to synthesise a final answer from all summaries.
/// </summary>
public class ResearchService : IResearchService
{
    private readonly IHttpClientFactory _http;
    private readonly OpenAiSettings _ai;
    private readonly ILogger<ResearchService> _logger;

    // Safety cap: never fetch more than this many sources per question.
    private const int MaxSources = 5;
    // Max characters of raw HTML we send to the summariser (to stay within token limits).
    private const int MaxPageChars = 12_000;

    public ResearchService(
        IHttpClientFactory http,
        IOptions<OpenAiSettings> ai,
        ILogger<ResearchService> logger)
    {
        _http   = http;
        _ai     = ai.Value;
        _logger = logger;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public async Task<ResearchResult> ResearchAsync(string? text, Stream? audio, string? audioFileName)
    {
        var aiClient   = _http.CreateClient("OpenAi");
        var webClient  = _http.CreateClient("Web");

        // 1. Resolve the question text.
        string question;
        if (audio is not null && !string.IsNullOrWhiteSpace(audioFileName))
        {
            question = await TranscribeAsync(aiClient, audio, audioFileName);
            _logger.LogInformation("Research transcript: {Q}", question);
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            question = text.Trim();
        }
        else
        {
            return new ResearchResult { Question = "(no input)", FinalAnswer = "No question was provided." };
        }

        var result = new ResearchResult { Question = question };

        // 2. Plan web requests.
        List<(string url, string rationale)> plan;
        try
        {
            plan = await PlanRequestsAsync(aiClient, question);
            _logger.LogInformation("Research plan: {Count} sources", plan.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to plan research requests");
            result.FinalAnswer = $"Could not plan research: {ex.Message}";
            return result;
        }

        // 3 & 4. Fetch each URL and summarise.
        foreach (var (url, rationale) in plan.Take(MaxSources))
        {
            var source = new ResearchSource { Url = url, Rationale = rationale };
            result.Sources.Add(source);

            string pageText;
            try
            {
                pageText = await FetchPageTextAsync(webClient, url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch {Url}", url);
                source.Success = false;
                source.Error   = $"Fetch failed: {ex.Message}";
                continue;
            }

            try
            {
                source.Summary = await SummarisePageAsync(aiClient, question, url, pageText);
                source.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to summarise {Url}", url);
                source.Success = false;
                source.Error   = $"Summarisation failed: {ex.Message}";
            }
        }

        // 5. Synthesise a final answer from all successful summaries.
        var goodSources = result.Sources.Where(s => s.Success).ToList();
        if (goodSources.Count > 0)
        {
            try
            {
                result.FinalAnswer = await SynthesiseFinalAnswerAsync(aiClient, question, goodSources);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to synthesise final answer");
                result.FinalAnswer = "Could not synthesise a final answer from the sources.";
            }
        }
        else
        {
            result.FinalAnswer = "None of the planned sources could be fetched or summarised.";
        }

        return result;
    }

    // ── Step 1: Whisper transcription ─────────────────────────────────────────

    private async Task<string> TranscribeAsync(HttpClient http, Stream audio, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, _ai.WhisperUrl);
        request.Headers.Add("api-key", _ai.ApiKey);
        request.Content = content;

        var response = await http.SendAsync(request);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Whisper error ({response.StatusCode}): {body}");

        var json = JsonNode.Parse(body);
        return json?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Whisper response missing 'text' field.");
    }

    // ── Step 2: GPT plans a list of URLs ──────────────────────────────────────

    private async Task<List<(string url, string rationale)>> PlanRequestsAsync(
        HttpClient http, string question)
    {
        var systemPrompt = """
            You are a research assistant. Given a user question, produce a JSON array of up to 5
            web requests that together will best answer the question.

            Each element must be:
            { "url": "<full URL including https://>", "rationale": "<one sentence why this helps>" }

            Rules:
            - Prefer direct, authoritative sources (official docs, Wikipedia, news sites, APIs).
            - Only include publicly accessible URLs that return meaningful content without login.
            - Respond with ONLY the JSON array — no markdown, no explanation.
            - If the question needs real-time data (weather, prices) use a free public API URL.
            """;

        var payload = new
        {
            temperature = 0.2,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = question },
            },
        };

        var gptResponse = await CallChatAsync(http, payload);

        // Strip optional markdown fences
        var cleaned = gptResponse.Trim();
        if (cleaned.StartsWith("```")) cleaned = string.Join('\n', cleaned.Split('\n').Skip(1));
        if (cleaned.EndsWith("```"))   cleaned = cleaned[..cleaned.LastIndexOf("```")];

        var array = JsonDocument.Parse(cleaned.Trim()).RootElement;
        if (array.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("GPT did not return a JSON array for the research plan.");

        var result = new List<(string, string)>();
        foreach (var el in array.EnumerateArray())
        {
            var url       = el.TryGetProperty("url",       out var u) ? u.GetString() ?? "" : "";
            var rationale = el.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "";
            if (!string.IsNullOrWhiteSpace(url))
                result.Add((url, rationale));
        }
        return result;
    }

    // ── Step 3: Fetch a URL and extract visible text ───────────────────────────

    private async Task<string> FetchPageTextAsync(HttpClient http, string url)
    {
        var response = await http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        // Very lightweight HTML → text: strip tags, collapse whitespace.
        var text = StripHtml(html);
        return text.Length > MaxPageChars ? text[..MaxPageChars] : text;
    }

    private static string StripHtml(string html)
    {
        // Remove script/style blocks first.
        html = System.Text.RegularExpressions.Regex.Replace(
            html, @"<(script|style)[^>]*>[\s\S]*?<\/\1>", " ",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        // Strip remaining tags.
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]+>", " ");
        // Collapse whitespace.
        html = System.Text.RegularExpressions.Regex.Replace(html, @"\s{2,}", " ");
        return html.Trim();
    }

    // ── Step 4: GPT summarises a single page ──────────────────────────────────

    private async Task<string> SummarisePageAsync(
        HttpClient http, string question, string url, string pageText)
    {
        var systemPrompt = $"""
            You are a research assistant. The user asked: "{question}"
            You have fetched the content from: {url}
            Extract and summarise ONLY the information from that page that is directly relevant
            to answering the user's question. Be concise (max 3 paragraphs).
            If the page contains no relevant information, say so in one sentence.
            """;

        var payload = new
        {
            temperature = 0.3,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = pageText },
            },
        };

        return await CallChatAsync(http, payload);
    }

    // ── Step 5: GPT synthesises a final answer ────────────────────────────────

    private async Task<string> SynthesiseFinalAnswerAsync(
        HttpClient http, string question, List<ResearchSource> sources)
    {
        var sourcesText = string.Join("\n\n---\n\n",
            sources.Select((s, i) => $"Source {i + 1} ({s.Url}):\n{s.Summary}"));

        var systemPrompt = $"""
            You are a research assistant. The user asked: "{question}"
            Below are summaries from several web sources. Write a clear, well-structured answer
            to the user's question, drawing on all relevant sources.
            - Be factual and concise.
            - Use plain language; no jargon unless necessary.
            - If sources contradict each other, note the disagreement.
            - Do NOT include raw URLs in the answer body; they are shown separately.
            """;

        var payload = new
        {
            temperature = 0.4,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = sourcesText },
            },
        };

        return await CallChatAsync(http, payload);
    }

    // ── Shared GPT helper ─────────────────────────────────────────────────────

    private async Task<string> CallChatAsync(HttpClient http, object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _ai.ChatUrl);
        request.Headers.Add("api-key", _ai.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        var body     = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GPT error ({response.StatusCode}): {body}");

        var json = JsonNode.Parse(body);
        return json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Unexpected GPT response format.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetAudioMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp3"  => "audio/mpeg",
            ".mp4"  => "audio/mp4",
            ".m4a"  => "audio/m4a",
            ".wav"  => "audio/wav",
            ".webm" => "audio/webm",
            ".ogg"  => "audio/ogg",
            ".flac" => "audio/flac",
            _       => "application/octet-stream",
        };
}
