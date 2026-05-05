using NokiaHome.Models;

namespace NokiaHome.Services.Agents;

/// <param name="Transcript">Raw text from Whisper.</param>
/// <param name="Actions">One entry per action extracted from the recording, in order.</param>
public record OrchestratorResult(
    string Transcript,
    List<VoiceActionResult> Actions);

public interface IOrchestratorService
{
    /// <summary>
    /// Transcribes the audio, splits the transcript into individual actions,
    /// and executes each one via the appropriate spoke.
    /// Unrecognized actions fall back to a journal entry.
    /// </summary>
    Task<OrchestratorResult> ProcessVoiceAsync(Stream audio, string fileName, DateTime referenceNow);
}
