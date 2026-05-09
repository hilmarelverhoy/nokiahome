using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class ResearchController : Controller
{
    private readonly IResearchService _research;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(IResearchService research, ILogger<ResearchController> logger)
    {
        _research = research;
        _logger   = logger;
    }

    // GET /Research
    public IActionResult Index() => View();

    // POST /Research/Ask
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)] // 50 MB — same limit as the voice controller
    public async Task<IActionResult> Ask(string? question, IFormFile? recording)
    {
        if (string.IsNullOrWhiteSpace(question) && (recording is null || recording.Length == 0))
        {
            TempData["Error"] = "Please type a question or upload an audio recording.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            Stream? audio    = null;
            string? fileName = null;

            if (recording is { Length: > 0 })
            {
                audio    = recording.OpenReadStream();
                fileName = recording.FileName;
            }

            await using (audio)
            {
                var result = await _research.ResearchAsync(question, audio, fileName);
                // Store in TempData as JSON so we can pass it to the result view.
                // For a production system you'd persist this; for now TempData is fine.
                TempData["ResearchResult"] = System.Text.Json.JsonSerializer.Serialize(result);
                return RedirectToAction(nameof(Result));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research pipeline failed");
            TempData["Error"] = $"Research failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET /Research/Result
    public IActionResult Result()
    {
        var json = TempData["ResearchResult"] as string;
        if (string.IsNullOrWhiteSpace(json))
            return RedirectToAction(nameof(Index));

        var result = System.Text.Json.JsonSerializer.Deserialize<NokiaHome.Models.Research.ResearchResult>(json);
        if (result is null)
            return RedirectToAction(nameof(Index));

        return View(result);
    }
}
