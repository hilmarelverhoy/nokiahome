using System.Text.Json;

namespace NokiaHome.Services.Agents;

/// <summary>
/// Spoke for "linear.*" intents.
/// Handles: linear.create (create a new issue in the configured team).
/// </summary>
public class LinearAgent : ISpecializedAgent
{
    public string Intent => "linear";

    private readonly ILinearService _linear;
    private readonly ILogger<LinearAgent> _logger;

    private static readonly string[] PriorityLabels = ["Ingen", "Haster", "Høy", "Middels", "Lav"];

    public LinearAgent(ILinearService linear, ILogger<LinearAgent> logger)
    {
        _linear = linear;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string transcript, JsonElement parameters, DateTime referenceNow)
    {
        var title       = GetString(parameters, "title");
        var description = GetString(parameters, "description");
        var priority    = parameters.TryGetProperty("priority", out var p) ? p.GetInt32() : 0;

        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("LinearAgent: tittel er påkrevd for å opprette en sak.");

        var issue = await _linear.CreateIssueAsync(
            title.Trim(),
            description?.Trim(),
            priority,
            stateId: null);

        _logger.LogInformation("LinearAgent: created issue {Id} — \"{Title}\"", issue.Id, issue.Title);

        var priorityLabel = priority >= 0 && priority < PriorityLabels.Length
            ? PriorityLabels[priority]
            : "Ingen";

        return $"Linear-sak \"{issue.Title}\" opprettet (prioritet: {priorityLabel}).";
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
