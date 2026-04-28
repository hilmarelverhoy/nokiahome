using Microsoft.AspNetCore.Mvc;
using NokiaHome.Models;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarService _calendar;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(ICalendarService calendar, ILogger<CalendarController> logger)
    {
        _calendar = calendar;
        _logger = logger;
    }

    // GET /Calendar
    public async Task<IActionResult> Index()
    {
        var events = await _calendar.GetEventsAsync();
        var vm = new CalendarIndexViewModel
        {
            Events = events,
            Form = new CreateEventForm
            {
                Start = DateTime.Today.AddHours(9),
                End   = DateTime.Today.AddHours(10),
            }
        };
        return View(vm);
    }

    // POST /Calendar/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEventForm form)
    {
        if (!ModelState.IsValid)
        {
            var events = await _calendar.GetEventsAsync();
            return View("Index", new CalendarIndexViewModel { Events = events, Form = form });
        }

        if (form.End <= form.Start && !form.AllDay)
        {
            ModelState.AddModelError(nameof(form.End), "End must be after start.");
            var events = await _calendar.GetEventsAsync();
            return View("Index", new CalendarIndexViewModel { Events = events, Form = form });
        }

        try
        {
            var ev = new CalendarEvent
            {
                Title       = form.Title.Trim(),
                Description = form.Description?.Trim(),
                Location    = form.Location?.Trim(),
                Start       = form.Start,
                End         = form.AllDay ? form.Start : form.End,
                AllDay      = form.AllDay,
            };

            await _calendar.AddEventAsync(ev);
            TempData["Success"] = $"\"{ev.Title}\" added to the calendar.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add calendar event.");
            TempData["Error"] = $"Could not save event: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST /Calendar/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _calendar.DeleteEventAsync(id);
            TempData["Success"] = "Event deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {Id}.", id);
            TempData["Error"] = $"Could not delete event: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET /Calendar/Feed
    /// Returns an iCalendar (.ics) feed that can be subscribed to from any
    /// calendar app (Google Calendar, Apple Calendar, Outlook, etc.).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Feed()
    {
        var ical = await _calendar.GetICalFeedAsync();
        return Content(ical, "text/calendar", System.Text.Encoding.UTF8);
    }
}
