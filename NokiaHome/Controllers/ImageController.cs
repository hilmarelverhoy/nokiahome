using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class ImageController : Controller
{
    private readonly IImageResizeService _resize;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IImageResizeService resize, ILogger<ImageController> logger)
    {
        _resize = resize;
        _logger = logger;
    }

    // GET /Image
    public IActionResult Index()
    {
        return View();
    }

    // POST /Image/Resize
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resize(IFormFile file, int quality = 75)
    {
        if (file is not { Length: > 0 })
        {
            TempData["Error"] = "Please select an image file.";
            return View("Index");
        }

        if (quality < 1 || quality > 100)
        {
            quality = 75;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var (data, contentType) = await _resize.ResizeToQvgaAsync(stream, file.FileName, quality);

            var downloadName = Path.GetFileNameWithoutExtension(file.FileName) + "_qvga.jpg";
            return File(data, contentType, downloadName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize image.");
            TempData["Error"] = $"Could not resize image: {ex.Message}";
            return View("Index");
        }
    }
}