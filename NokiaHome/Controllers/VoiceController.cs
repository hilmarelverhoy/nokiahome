using Microsoft.AspNetCore.Mvc;
using NokiaHome.Models;
using NokiaHome.Services;
using NokiaHome.Services.Agents;

namespace NokiaHome.Controllers;

public class VoiceController : Controller
{
    private readonly IOrchestratorService _orchestrator;
    private readonly IVoiceLogService _log;
    private readonly ILogger<VoiceController> _logger;

    public VoiceController(
        IOrchestratorService orchestrator,
        IVoiceLogService log,
        ILogger<VoiceController> logger)
    {
        _orchestrator = orchestrator;
        _log = log;
        _logger = logger;
    }

    // GET /Voice
    public IActionResult Index() => View();

    // POST /Voice/Command
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Command(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Ingen lydfil mottatt.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _orchestrator.ProcessVoiceAsync(stream, file.FileName, DateTime.Now);

            var entry = new VoiceCommandLog
            {
                Transcript = result.Transcript,
                Actions    = result.Actions,
            };

            await _log.AddLogAsync(entry);

            return RedirectToAction(nameof(Result), new { id = entry.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice command processing failed for {FileName}", file.FileName);
            TempData["Error"] = $"Feil under behandling: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET /Voice/Result/{id}
    public async Task<IActionResult> Result(string id)
    {
        var entry = await _log.GetLogAsync(id);
        if (entry is null)
            return RedirectToAction(nameof(Index));

        return View(entry);
    }

    // POST /Voice/Feedback
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(VoiceActionFeedbackForm form)
    {
        if (!string.IsNullOrWhiteSpace(form.LogId) && !string.IsNullOrWhiteSpace(form.Rating))
            await _log.SaveActionFeedbackAsync(form.LogId, form.ActionIndex, form.Rating, form.Comment);

        // Return to the same result page so the user can rate remaining actions
        return RedirectToAction(nameof(Result), new { id = form.LogId });
    }
}
