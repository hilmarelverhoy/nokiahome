using Microsoft.AspNetCore.Mvc;
using NokiaHome.Models;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class JournalController : Controller
{
    private readonly IJournalService _journal;
    private readonly ILogger<JournalController> _logger;

    public JournalController(IJournalService journal, ILogger<JournalController> logger)
    {
        _journal = journal;
        _logger = logger;
    }

    // GET /Journal
    public async Task<IActionResult> Index()
    {
        var entries = await _journal.GetEntriesAsync();
        var vm = new JournalIndexViewModel { Entries = entries };
        return View(vm);
    }

    // POST /Journal/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateJournalEntryForm form, IFormFile? file)
    {
        // If a .txt file was uploaded, use its content as the body
        if (file is { Length: > 0 })
        {
            using var reader = new StreamReader(file.OpenReadStream());
            form.Body = await reader.ReadToEndAsync();

            // Use filename as title if title was not supplied
            if (string.IsNullOrWhiteSpace(form.Title))
                form.Title = Path.GetFileNameWithoutExtension(file.FileName);
        }

        if (!ModelState.IsValid)
        {
            var entries = await _journal.GetEntriesAsync();
            return View("Index", new JournalIndexViewModel { Entries = entries, Form = form });
        }

        if (string.IsNullOrWhiteSpace(form.Body))
        {
            ModelState.AddModelError(nameof(form.Body), "Entry body cannot be empty.");
            var entries = await _journal.GetEntriesAsync();
            return View("Index", new JournalIndexViewModel { Entries = entries, Form = form });
        }

        try
        {
            var entry = new JournalEntry
            {
                Title = form.Title.Trim(),
                Body  = form.Body.Trim(),
            };

            await _journal.AddEntryAsync(entry);
            TempData["Success"] = $"\"{entry.Title}\" saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save journal entry.");
            TempData["Error"] = $"Could not save entry: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Journal/Detail/{id}
    public async Task<IActionResult> Detail(string id)
    {
        var entry = await _journal.GetEntryAsync(id);
        if (entry is null)
            return NotFound();

        return View(entry);
    }

    // POST /Journal/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _journal.DeleteEntryAsync(id);
            TempData["Success"] = "Entry deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete journal entry {Id}.", id);
            TempData["Error"] = $"Could not delete entry: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
