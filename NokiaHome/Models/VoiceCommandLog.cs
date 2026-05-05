namespace NokiaHome.Models;

/// <summary>
/// Result of a single extracted action within one voice recording.
/// A recording may produce several of these.
/// </summary>
public class VoiceActionResult
{
    /// <summary>Full intent string from GPT, e.g. "calendar.create", "unknown".</summary>
    public string Intent { get; set; } = string.Empty;

    /// <summary>Norwegian sentence describing what was done.</summary>
    public string ActionSummary { get; set; } = string.Empty;

    /// <summary>Whether this specific action succeeded.</summary>
    public bool Success { get; set; }

    // ── Per-action feedback ─────────────────────────────────────────────────

    /// <summary>"thumbs_up" | "thumbs_down" | null</summary>
    public string? FeedbackRating { get; set; }

    public string? FeedbackComment { get; set; }

    public DateTime? FeedbackAt { get; set; }
}

/// <summary>
/// Persisted record of one voice recording. Contains the raw transcript
/// and the list of actions the orchestrator extracted and executed.
/// </summary>
public class VoiceCommandLog
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Raw text produced by Whisper.</summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>All actions extracted from this recording, in order.</summary>
    public List<VoiceActionResult> Actions { get; set; } = [];
}

/// <summary>Feedback form submitted for one action on the Result page.</summary>
public class VoiceActionFeedbackForm
{
    public string LogId { get; set; } = string.Empty;

    /// <summary>Zero-based index into VoiceCommandLog.Actions.</summary>
    public int ActionIndex { get; set; }

    /// <summary>"thumbs_up" or "thumbs_down"</summary>
    public string Rating { get; set; } = string.Empty;

    public string? Comment { get; set; }
}
