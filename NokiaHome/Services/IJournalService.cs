using NokiaHome.Models;

namespace NokiaHome.Services;

public interface IJournalService
{
    /// <summary>Returns all entries sorted by creation date descending (newest first).</summary>
    Task<IReadOnlyList<JournalEntry>> GetEntriesAsync();

    /// <summary>Returns a single entry by ID, or null if not found.</summary>
    Task<JournalEntry?> GetEntryAsync(string id);

    Task AddEntryAsync(JournalEntry entry);

    Task DeleteEntryAsync(string id);
}
