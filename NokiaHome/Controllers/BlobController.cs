using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class BlobController : Controller
{
    private readonly IBlobStorageService _blobStorage;
    private readonly IPdfImageExtractionService _pdfImageExtraction;
    private readonly IPdfTextExtractionService _pdfTextExtraction;
    private readonly ILogger<BlobController> _logger;

    public BlobController(
        IBlobStorageService blobStorage,
        IPdfImageExtractionService pdfImageExtraction,
        IPdfTextExtractionService pdfTextExtraction,
        ILogger<BlobController> logger)
    {
        _blobStorage = blobStorage;
        _pdfImageExtraction = pdfImageExtraction;
        _pdfTextExtraction = pdfTextExtraction;
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
            // Always store the original file first
            await using (var stream = file.OpenReadStream())
                await _blobStorage.UploadAsync(file.FileName, stream, file.ContentType);

            TempData["Success"] = $"{file.FileName} uploaded successfully.";

            // Run PDF processing pipeline when a PDF is uploaded
            if (IsPdf(file))
                await ProcessPdfAsync(file);
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

    // POST /Blob/Unzip
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unzip(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Blob name is required.");

        try
        {
            var (zipStream, _) = await _blobStorage.DownloadAsync(name);

            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);

            var folder = Path.GetFileNameWithoutExtension(name);
            var count = 0;

            foreach (var entry in archive.Entries)
            {
                // Skip directory entries
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                var blobName = $"{folder}/{entry.FullName}";
                var contentType = GetContentType(entry.Name);

                await using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                await entryStream.CopyToAsync(ms);
                ms.Position = 0;

                await _blobStorage.UploadAsync(blobName, ms, contentType);
                count++;
            }

            TempData["Success"] = $"Extracted {count} file(s) from \"{name}\" into \"{folder}/\".";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unzip blob {Name}", name);
            TempData["Error"] = $"Unzip failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".png"  => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"  => "image/gif",
        ".webp" => "image/webp",
        ".pdf"  => "application/pdf",
        ".json" => "application/json",
        ".txt"  => "text/plain",
        ".csv"  => "text/csv",
        ".html" => "text/html",
        ".xml"  => "application/xml",
        _       => "application/octet-stream",
    };

    // ---------------------------------------------------------------------------
    // PDF pipeline
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Extracts images and text/metadata from a PDF and stores each artefact
    /// as its own blob under a folder named after the PDF file.
    ///
    /// Blob layout:
    ///   {pdfBaseName}/info.json          — metadata + full page text (pretty JSON)
    ///   {pdfBaseName}/page1_image0.png   — extracted images, one blob each
    /// </summary>
    private async Task ProcessPdfAsync(IFormFile file)
    {
        var baseName = Path.GetFileNameWithoutExtension(file.FileName);

        // Run both extractions concurrently from independent memory copies
        var (imageBytes, textBytes) = await ReadTwoCopiesAsync(file);

        var imageTask = _pdfImageExtraction.ExtractImagesAsync(new MemoryStream(imageBytes));
        var textTask  = _pdfTextExtraction.ExtractAsync(new MemoryStream(textBytes), file.FileName);

        await Task.WhenAll(imageTask, textTask);

        var images   = imageTask.Result;
        var docInfo  = textTask.Result;

        // Store info.json
        var json     = JsonSerializer.Serialize(docInfo, new JsonSerializerOptions { WriteIndented = true });
        var jsonBlob = $"{baseName}/info.json";
        await _blobStorage.UploadAsync(
            jsonBlob,
            new MemoryStream(Encoding.UTF8.GetBytes(json)),
            "application/json");

        _logger.LogInformation("Stored PDF info JSON at {Blob}", jsonBlob);

        // Store images
        if (images.Count > 0)
        {
            var uploadTasks = images.Select(image =>
                _blobStorage.UploadAsync(
                    $"{baseName}/{image.FileName}",
                    new MemoryStream(image.Data),
                    image.ContentType));

            await Task.WhenAll(uploadTasks);

            _logger.LogInformation("Stored {Count} image(s) from {FileName}", images.Count, file.FileName);
        }
        else
        {
            _logger.LogInformation("No images found in {FileName}", file.FileName);
        }
    }

    private static bool IsPdf(IFormFile file) =>
        string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
        || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the uploaded file into two independent byte arrays so the image
    /// extractor and the text extractor can each get a fresh, seekable stream.
    /// </summary>
    private static async Task<(byte[] First, byte[] Second)> ReadTwoCopiesAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();
        return (bytes, bytes); // same buffer is fine — both readers are sequential and non-overlapping
    }
}
