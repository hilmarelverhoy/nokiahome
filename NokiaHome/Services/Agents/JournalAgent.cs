using System.Text.Json;
using NokiaHome.Models;

namespace NokiaHome.Services.Agents;

/// <summary>
/// Spoke for "journal.*" intents.
/// Handles: journal.create
/// Also used internally by the orchestrator as the fallback for unrecognized commands.
/// </summary>
public class JournalAgent : ISpecializedAgent
{
    public string Intent => "journal";

    private readonly IJournalService _journal;
    private readonly ILogger<JournalAgent> _logger;

    public JournalAgent(IJournalService journal, ILogger<JournalAgent> logger)
    {
        _journal = journal;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string transcript, JsonElement parameters, DateTime referenceNow)
    {
        var title = GetString(parameters, "title");
        var body  = GetString(parameters, "body");

        var entry = new JournalEntry
        {
            Title = string.IsNullOrWhiteSpace(title)
                ? $"Notat {referenceNow:dd.MM.yyyy HH:mm}"
                : title.Trim(),
            Body = string.IsNullOrWhiteSpace(body) ? transcript : body,
        };

        await _journal.AddEntryAsync(entry);
        _logger.LogInformation("JournalAgent: created \"{Title}\"", entry.Title);

        return $"Dagboknotat \"{entry.Title}\" lagret.";
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
