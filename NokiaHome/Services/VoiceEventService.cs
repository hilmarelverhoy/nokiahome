using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Settings;

namespace NokiaHome.Services;

public class VoiceEventService : IVoiceEventService
{
    private readonly HttpClient _http;
    private readonly ILogger<VoiceEventService> _logger;
    private readonly OpenAiSettings _settings;

    public VoiceEventService(
        HttpClient http,
        IOptions<OpenAiSettings> settings,
        ILogger<VoiceEventService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // Transcription — Azure OpenAI Whisper
    // -------------------------------------------------------------------------

    public async Task<string> TranscribeAsync(Stream audio, string fileName)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.WhisperUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = content;

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Whisper API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Transcription failed ({response.StatusCode}): {body}");
        }

        var json = JsonNode.Parse(body);
        return json?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Whisper response missing 'text' field.");
    }

    // -------------------------------------------------------------------------
    // Event extraction — Azure OpenAI GPT
    // -------------------------------------------------------------------------

    public async Task<CreateEventForm> ParseEventAsync(string transcription, DateTime referenceNow)
    {
        var systemPrompt = $$"""
            You are a calendar assistant. Extract a single calendar event from the user's voice note.
            Today's date and time is: {{referenceNow:yyyy-MM-dd HH:mm}} (local time).
            
            Reply ONLY with a valid JSON object — no markdown, no explanation — using exactly these fields:
            {
              "title":       "<string, required>",
              "description": "<string or null>",
              "location":    "<string or null>",
              "start":       "<ISO 8601 local datetime, e.g. 2026-04-29T09:00:00>",
              "end":         "<ISO 8601 local datetime, same format>",
              "allDay":      <true|false>
            }
            
            Rules:
            - Resolve relative expressions like "tomorrow", "next Friday", "in two weeks" against today's date.
            - If no end time is mentioned, set end = start + 1 hour (or start + 1 day for all-day events).
            - If only a date is mentioned with no time, set allDay = true and use midnight for start/end.
            - Title must never be empty; infer one from context if the speaker did not state one explicitly.
            """;

        // Azure OpenAI does not require the "model" field — deployment is set in the URL
        var payload = new
        {
            temperature = 0,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = transcription },
            },
        };

        var requestBody = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ChatUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Chat completions API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Event parsing failed ({response.StatusCode}): {body}");
        }

        var json = JsonNode.Parse(body);
        var messageContent = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>()
                             ?? throw new InvalidOperationException("Unexpected GPT response shape.");

        _logger.LogInformation("GPT event JSON: {Json}", messageContent);

        return DeserializeEventForm(messageContent);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static CreateEventForm DeserializeEventForm(string json)
    {
        var node = JsonNode.Parse(json)
                   ?? throw new InvalidOperationException("GPT returned invalid JSON.");

        var start  = ParseDateTime(node["start"]);
        var end    = ParseDateTime(node["end"]);
        var allDay = node["allDay"]?.GetValue<bool>() ?? false;

        return new CreateEventForm
        {
            Title       = node["title"]?.GetValue<string>()?.Trim() ?? "Untitled event",
            Description = node["description"]?.GetValue<string>()?.Trim(),
            Location    = node["location"]?.GetValue<string>()?.Trim(),
            Start       = start,
            End         = end > start ? end : start.AddHours(1),
            AllDay      = allDay,
        };
    }

    private static DateTime ParseDateTime(JsonNode? node)
    {
        var raw = node?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(raw))
            return DateTime.Now.Date.AddHours(9);

        return DateTime.TryParse(raw, out var dt) ? dt : DateTime.Now.Date.AddHours(9);
    }

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
