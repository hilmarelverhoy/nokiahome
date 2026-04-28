using System.Text;
using System.Text.Json;
using Azure;
using NokiaHome.Models;
using NokiaHome.Settings;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace NokiaHome.Services;

/// <summary>
/// Persists calendar events as a single JSON blob at "calendar/events.json"
/// and exposes an iCalendar (.ics) feed generated on demand.
/// </summary>
public class CalendarService : ICalendarService
{
    private const string BlobName = "calendar/events.json";

    private readonly BlobContainerClient _container;
    private readonly ILogger<CalendarService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public CalendarService(IOptions<BlobStorageSettings> options, ILogger<CalendarService> logger)
    {
        var settings = options.Value;
        var serviceClient = new BlobServiceClient(settings.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
        _logger = logger;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync()
    {
        var events = await LoadAsync();
        return events.OrderBy(e => e.Start).ToList();
    }

    public async Task AddEventAsync(CalendarEvent calendarEvent)
    {
        var events = await LoadAsync();
        events.Add(calendarEvent);
        await SaveAsync(events);
    }

    public async Task DeleteEventAsync(string id)
    {
        var events = await LoadAsync();
        var removed = events.RemoveAll(e => e.Id == id);
        if (removed > 0)
            await SaveAsync(events);
    }

    public async Task<string> GetICalFeedAsync()
    {
        var events = await GetEventsAsync();
        return BuildICal(events);
    }

    // ---------------------------------------------------------------------------
    // Storage helpers
    // ---------------------------------------------------------------------------

    private async Task<List<CalendarEvent>> LoadAsync()
    {
        try
        {
            var blob = _container.GetBlobClient(BlobName);
            var response = await blob.DownloadStreamingAsync();
            using var reader = new StreamReader(response.Value.Content);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<List<CalendarEvent>>(json, JsonOptions) ?? [];
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load calendar events from blob storage.");
            return [];
        }
    }

    private async Task SaveAsync(List<CalendarEvent> events)
    {
        var json = JsonSerializer.Serialize(events, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var blob = _container.GetBlobClient(BlobName);
        await blob.UploadAsync(
            new MemoryStream(bytes),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } });
    }

    // ---------------------------------------------------------------------------
    // iCalendar generation
    // ---------------------------------------------------------------------------

    private static string BuildICal(IReadOnlyList<CalendarEvent> events)
    {
        var sb = new StringBuilder();

        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//NokiaHome//Calendar//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine("X-WR-CALNAME:NokiaHome");

        foreach (var ev in events)
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{ev.Id}@nokiahome");
            sb.AppendLine($"DTSTAMP:{ev.CreatedAt.ToUniversalTime():yyyyMMddTHHmmssZ}");

            if (ev.AllDay)
            {
                sb.AppendLine($"DTSTART;VALUE=DATE:{ev.Start:yyyyMMdd}");
                sb.AppendLine($"DTEND;VALUE=DATE:{ev.End.AddDays(1):yyyyMMdd}");
            }
            else
            {
                sb.AppendLine($"DTSTART:{ev.Start.ToUniversalTime():yyyyMMddTHHmmssZ}");
                sb.AppendLine($"DTEND:{ev.End.ToUniversalTime():yyyyMMddTHHmmssZ}");
            }

            sb.AppendLine($"SUMMARY:{Escape(ev.Title)}");

            if (!string.IsNullOrWhiteSpace(ev.Description))
                sb.AppendLine($"DESCRIPTION:{Escape(ev.Description)}");

            if (!string.IsNullOrWhiteSpace(ev.Location))
                sb.AppendLine($"LOCATION:{Escape(ev.Location)}");

            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    /// <summary>Escapes special characters per RFC 5545.</summary>
    private static string Escape(string value) =>
        value.Replace("\\", "\\\\")
             .Replace(";", "\\;")
             .Replace(",", "\\,")
             .Replace("\r\n", "\\n")
             .Replace("\n", "\\n");
}
