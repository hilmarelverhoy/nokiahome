using System.Text.Json;
using NokiaHome.Models;

namespace NokiaHome.Services.Agents;

/// <summary>
/// Spoke for "calendar.*" intents.
/// Currently handles: calendar.create
/// </summary>
public class CalendarAgent : ISpecializedAgent
{
    public string Intent => "calendar";

    private readonly ICalendarService _calendar;
    private readonly ILogger<CalendarAgent> _logger;

    public CalendarAgent(ICalendarService calendar, ILogger<CalendarAgent> logger)
    {
        _calendar = calendar;
        _logger = logger;
    }

    public async Task<string> ExecuteAsync(string transcript, JsonElement parameters, DateTime referenceNow)
    {
        var title       = GetString(parameters, "title");
        var description = GetString(parameters, "description");
        var location    = GetString(parameters, "location");
        var allDay      = parameters.TryGetProperty("allDay", out var a) && a.GetBoolean();

        var defaultStart = referenceNow.Date.AddHours(9);
        var start = ParseDateTime(parameters, "start", defaultStart);
        var end   = ParseDateTime(parameters, "end",   start.AddHours(1));

        var ev = new CalendarEvent
        {
            Title       = string.IsNullOrWhiteSpace(title) ? "Hendelse uten tittel" : title.Trim(),
            Description = description?.Trim(),
            Location    = location?.Trim(),
            Start       = start,
            End         = end > start ? end : start.AddHours(1),
            AllDay      = allDay,
        };

        await _calendar.AddEventAsync(ev);
        _logger.LogInformation("CalendarAgent: created \"{Title}\" at {Start}", ev.Title, ev.Start);

        var when = allDay
            ? ev.Start.ToString("d. MMMM yyyy", new System.Globalization.CultureInfo("nb-NO"))
            : ev.Start.ToString("d. MMMM yyyy 'kl.' HH:mm", new System.Globalization.CultureInfo("nb-NO"));

        return $"Hendelse \"{ev.Title}\" opprettet {when}.";
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static DateTime ParseDateTime(JsonElement el, string name, DateTime fallback)
    {
        if (!el.TryGetProperty(name, out var node)) return fallback;
        var raw = node.GetString();
        return DateTime.TryParse(raw, out var dt) ? dt : fallback;
    }
}
