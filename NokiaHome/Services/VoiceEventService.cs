using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NokiaHome.Settings;

namespace NokiaHome.Services;

/// <summary>
/// Thin wrapper around the Azure OpenAI Whisper endpoint.
/// The transcription logic mirrors <c>OrchestratorService.TranscribeAsync</c> but is
/// exposed here as a standalone service so that controllers (e.g. JournalController)
/// can transcribe audio without going through the full voice-command pipeline.
/// </summary>
public class VoiceEventService : IVoiceEventService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiSettings _settings;
    private readonly ILogger<VoiceEventService> _logger;

    public VoiceEventService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiSettings> settings,
        ILogger<VoiceEventService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> TranscribeAsync(Stream audio, string fileName)
    {
        var http = _httpClientFactory.CreateClient("OpenAi");

        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(audio);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        // Hint Whisper that the audio is Norwegian.
        content.Add(new StringContent("no"), "language");

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.WhisperUrl);
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = content;

        var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Whisper feil ({response.StatusCode}): {body}");

        _logger.LogInformation("Whisper transcription completed for {FileName}", fileName);

        var json = JsonNode.Parse(body);
        return json?["text"]?.GetValue<string>()
               ?? throw new InvalidOperationException("Whisper-svaret mangler 'text'-felt.");
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
