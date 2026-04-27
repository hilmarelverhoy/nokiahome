using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class BlobController : Controller
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<BlobController> _logger;

    public BlobController(IBlobStorageService blobStorage, ILogger<BlobController> logger)
    {
        _blobStorage = blobStorage;
        _logger = logger;
    }

    // GET /Blob
    public async Task<IActionResult> Index()
    {
        var blobs = await _blobStorage.ListBlobsAsync();
        return View(blobs);
    }

    // POST /Blob/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _blobStorage.UploadAsync(file.FileName, stream, file.ContentType);
            TempData["Success"] = $"{file.FileName} uploaded successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {FileName}", file.FileName);
            TempData["Error"] = $"Upload failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Blob/Download?name=...
    public async Task<IActionResult> Download(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Blob name is required.");

        try
        {
            var (content, contentType) = await _blobStorage.DownloadAsync(name);
            return File(content, contentType, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {Name}", name);
            TempData["Error"] = $"Download failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST /Blob/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Blob name is required.");

        try
        {
            await _blobStorage.DeleteAsync(name);
            TempData["Success"] = $"{name} deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {Name}", name);
            TempData["Error"] = $"Delete failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
