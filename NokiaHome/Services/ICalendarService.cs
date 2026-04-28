using NokiaHome.Models;

namespace NokiaHome.Services;

public interface ICalendarService
{
    /// <summary>Returns all events sorted by start date ascending.</summary>
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync();

    Task AddEventAsync(CalendarEvent calendarEvent);

    Task DeleteEventAsync(string id);

    /// <summary>Renders all events as an iCalendar (.ics) string.</summary>
    Task<string> GetICalFeedAsync();
}
