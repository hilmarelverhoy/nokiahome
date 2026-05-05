using NokiaHome.Models;

namespace NokiaHome.Services;

public interface IVoiceLogService
{
    /// <summary>Returns all log entries, newest first.</summary>
    Task<IReadOnlyList<VoiceCommandLog>> GetLogsAsync();

    /// <summary>Returns a single entry by ID, or null.</summary>
    Task<VoiceCommandLog?> GetLogAsync(string id);

    /// <summary>Appends a new log entry.</summary>
    Task AddLogAsync(VoiceCommandLog entry);

    /// <summary>
    /// Saves feedback for one action within a log entry.
    /// <paramref name="actionIndex"/> is the zero-based index into <see cref="VoiceCommandLog.Actions"/>.
    /// No-op if the log ID or index is not found.
    /// </summary>
    Task SaveActionFeedbackAsync(string logId, int actionIndex, string rating, string? comment);
}
