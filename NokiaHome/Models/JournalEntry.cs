using System.ComponentModel.DataAnnotations;

namespace NokiaHome.Models;

public class JournalEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class JournalIndexViewModel
{
    public IReadOnlyList<JournalEntry> Entries { get; init; } = [];
    public CreateJournalEntryForm Form { get; init; } = new();
}

public class CreateJournalEntryForm
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100_000)]
    public string? Body { get; set; }
}
