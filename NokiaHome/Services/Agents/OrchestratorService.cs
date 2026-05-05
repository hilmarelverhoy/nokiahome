using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Settings;

namespace NokiaHome.Services.Agents;

/// <summary>
/// Hub of the hub-and-spoke voice command architecture.
///
/// Pipeline:
///   1. Transcribe audio via Azure OpenAI Whisper (language=no).
///   2. Send the transcript to GPT in a single call that splits it into an ordered
///      list of actions, each with an intent label and structured parameters.
///   3. Execute each action via the matching <see cref="ISpecializedAgent"/> spoke.
///   4. Actions whose intent is "unknown", or whose spoke fails, fall back to a journal note.
/// </summary>
public class OrchestratorService : IOrchestratorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiSettings _settings;
    private readonly IEnumerable<ISpecializedAgent> _agents;
    private readonly IJournalService _journal;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiSettings> settings,
        IEnumerable<ISpecializedAgent> agents,
        IJournalService journal,
        ILogger<OrchestratorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _agents = agents;
        _journal = journal;
        _logger = logger;
    }

    public async Task<OrchestratorResult> ProcessVoiceAsync(Stream audio, string fileName, DateTime referenceNow)
    {
        var http = _httpClientFactory.CreateClient("OpenAi");

        // 1. Transcribe
        string transcript;
        try
        {
            transcript = await TranscribeAsync(http, audio, fileName);
            _logger.LogInformation("Transcription: {Transcript}", transcript);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Whisper transcription failed for {FileName}", fileName);
            var errorAction = new VoiceActionResult
            {
                Intent        = "error",
                ActionSummary = $"Transkribering feilet: {ex.Message}",
                Success       = false,
            };
            return new OrchestratorResult("", [errorAction]);
        }

        // 2. Split transcript into ordered list of { intent, params } via one GPT call
        List<(string intent, JsonElement parameters)> extractedActions;
        try
        {
            extractedActions = await ExtractActionsAsync(http, transcript, referenceNow);
            _logger.LogInformation("Extracted {Count} action(s): {Intents}",
                extractedActions.Count,
                string.Join(", ", extractedActions.Select(a => a.intent)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT extraction failed for transcript: {Transcript}", transcript);
            extractedActions = [("unknown", JsonDocument.Parse("{}").RootElement)];
        }

        // 3. Execute each action
        var results = new List<VoiceActionResult>();

        foreach (var (intent, parameters) in extractedActions)
        {
            var intentPrefix = intent.Split('.')[0];
            var agent = intent != "unknown"
                ? _agents.FirstOrDefault(a => a.Intent == intentPrefix)
                : null;

            if (agent is null)
            {
                var summary = await StoreFallback(transcript, referenceNow, results.Count == 0);
                results.Add(new VoiceActionResult
                {
                    Intent        = intent,
                    ActionSummary = summary,
                    Success       = true,
                });
                continue;
            }

            try
            {
                var summary = await agent.ExecuteAsync(transcript, parameters, referenceNow);
                results.Add(new VoiceActionResult
                {
                    Intent        = intent,
                    ActionSummary = summary,
                    Success       = true,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {Intent} failed", intent);
                var summary = await StoreFallback(transcript, referenceNow, results.Count == 0);
                results.Add(new VoiceActionResult
                {
                    Intent        = intent,
                    ActionSummary = summary,
                    Success       = false,
                });
            }
        }

        return new OrchestratorResult(transcript, results);
    }

    // ── Whisper ───────────────────────────────────────────────────────────────

    private async Task<string> TranscribeAsync(HttpClient http, Stream audio, string fileName)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        // Tell Whisper the audio is Norwegian so it uses the correct language model.
        content.Add(new StringContent("no"), "language");

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.WhisperUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = content;

        var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Whisper feil ({response.StatusCode}): {body}");

        var json = JsonNode.Parse(body);
        return json?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Whisper-svaret mangler 'text'-felt.");
    }

    // ── GPT: split into actions ───────────────────────────────────────────────

    private async Task<List<(string intent, JsonElement parameters)>> ExtractActionsAsync(
        HttpClient http, string transcript, DateTime referenceNow)
    {
        var systemPrompt = $$$"""
            Du er en talekommando-ruter for et personlig hjemmedashbord.
            Brukeren snakker norsk. Dagens dato og tid er: {{{referenceNow:yyyy-MM-dd HH:mm}}} (lokal tid).

            Et opptak kan inneholde én eller flere kommandoer. Identifiser ALLE kommandoer i opptaket og
            returner dem som en JSON-array — én handling per objekt, i den rekkefølgen de ble nevnt.

            Svar KUN med en gyldig JSON-array — ingen markdown, ingen forklaring — i dette formatet:
            [
              { "intent": "<se nedenfor>", "params": { ... } },
              ...
            ]

            Støttede intensjoner og deres parametere:

            calendar.create — opprett en kalenderhendelse
            {
              "title":       "<string, påkrevd>",
              "description": "<string eller null>",
              "location":    "<string eller null>",
              "start":       "<ISO 8601 lokal datetime, f.eks. 2026-05-06T09:00:00>",
              "end":         "<ISO 8601 lokal datetime>",
              "allDay":      <true|false>
            }

            journal.create — skriv et dagboknotat
            {
              "title": "<string eller null>",
              "body":  "<string, relevant del av transkripsjonen>"
            }

            linear.create — opprett en oppgave eller sak i prosjektstyring
            {
              "title":       "<string, påkrevd>",
              "description": "<string eller null>",
              "priority":    <0=ingen, 1=haster, 2=høy, 3=middels, 4=lav>
            }

            unknown — en setning kunne ikke klassifiseres
            {}

            Regler for norsk datooppløsning:
            - i dag = today, i morgen = tomorrow, i overmorgen = day after tomorrow
            - neste [mandag/tirsdag/onsdag/torsdag/fredag/lørdag/søndag] = next occurrence of that weekday
            - om X dager / om X uker / om X måneder = in X days/weeks/months
            - neste uke = next week (same weekday)
            - Hvis ingen klokkeslett er nevnt, bruk 09:00 og sett allDay=false.
            - Hvis ingen sluttid er nevnt, sett end = start + 1 time.
            - Hvis bare dato er nevnt uten klokkeslett og konteksten tyder på heldagshendelse, sett allDay=true.

            Generelle regler:
            - Del opp opptaket i separate handlinger. Ikke slå sammen to ulike kommandoer til én.
            - Hvis ingen handling kan identifiseres, returner ett enkelt unknown-element: [{"intent":"unknown","params":{}}]
            - Skriv aldri noe annet enn JSON-arrayen.
            """;

        var payload = new
        {
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = transcript },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ChatUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GPT feil ({response.StatusCode}): {body}");

        var json = JsonNode.Parse(body);
        var content = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
                      ?? throw new InvalidOperationException("Uventet GPT-svarformat.");

        _logger.LogInformation("GPT actions response: {Response}", content);

        var array = JsonDocument.Parse(content).RootElement;
        if (array.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("GPT returnerte ikke en JSON-array.");

        var result = new List<(string, JsonElement)>();
        foreach (var element in array.EnumerateArray())
        {
            var intent     = element.TryGetProperty("intent", out var i) ? i.GetString() ?? "unknown" : "unknown";
            var parameters = element.TryGetProperty("params", out var p) ? p : JsonDocument.Parse("{}").RootElement;
            result.Add((intent, parameters));
        }

        return result.Count > 0 ? result : [("unknown", JsonDocument.Parse("{}").RootElement)];
    }

    // ── Fallback ──────────────────────────────────────────────────────────────

    /// <param name="isOnlyAction">When true and the whole recording was unrecognized, store the full transcript.</param>
    private async Task<string> StoreFallback(string transcript, DateTime referenceNow, bool isOnlyAction)
    {
        var title = $"Taleopptak {referenceNow:dd.MM.yyyy HH:mm}";
        var entry = new JournalEntry
        {
            Title = title,
            Body  = isOnlyAction ? transcript : $"[ukjent kommando] {transcript}",
        };
        await _journal.AddEntryAsync(entry);
        _logger.LogInformation("Stored unrecognized action as journal note.");
        return $"Forstod ikke kommandoen — lagret som notat \"{title}\".";
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
