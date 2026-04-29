using Microsoft.AspNetCore.Mvc;
using NokiaHome.Models;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarService _calendar;
    private readonly IVoiceEventService _voiceEvent;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        ICalendarService calendar,
        IVoiceEventService voiceEvent,
        ILogger<CalendarController> logger)
    {
        _calendar = calendar;
        _voiceEvent = voiceEvent;
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

    // POST /Calendar/VoiceCreate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VoiceCreate(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select an audio file.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var transcription = await _voiceEvent.TranscribeAsync(stream, file.FileName);

            var form = await _voiceEvent.ParseEventAsync(transcription, DateTime.Now);

            return View("VoiceConfirm", new VoiceConfirmViewModel
            {
                Transcription = transcription,
                Form          = form,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice event creation failed for {FileName}", file.FileName);
            TempData["Error"] = $"Could not process recording: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}
