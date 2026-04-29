using NokiaHome.Models;

namespace NokiaHome.Services;

public interface IVoiceEventService
{
    /// <summary>Sends audio to OpenAI Whisper and returns the transcription text.</summary>
    Task<string> TranscribeAsync(Stream audio, string fileName);

    /// <summary>
    /// Sends a transcription to GPT and returns a pre-filled CalendarEvent.
    /// The model resolves relative dates (e.g. "tomorrow", "next Friday") against
    /// <paramref name="referenceNow"/>.
    /// </summary>
    Task<CreateEventForm> ParseEventAsync(string transcription, DateTime referenceNow);
}
