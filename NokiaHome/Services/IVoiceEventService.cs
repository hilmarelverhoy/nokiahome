namespace NokiaHome.Services;

/// <summary>
/// Thin wrapper around the Azure OpenAI Whisper endpoint.
/// Used by <see cref="NokiaHome.Controllers.JournalController"/> to transcribe
/// an uploaded audio file directly into a journal entry, without going through
/// the full voice-command orchestration pipeline.
/// </summary>
public interface IVoiceEventService
{
    /// <summary>Transcribes <paramref name="audio"/> and returns the plain-text transcript.</summary>
    Task<string> TranscribeAsync(Stream audio, string fileName);
}
