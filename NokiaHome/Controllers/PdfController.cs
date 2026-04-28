using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers;

public class PdfController : Controller
{
    private readonly IPdfImageExtractionService _pdfImageExtraction;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<PdfController> _logger;

    public PdfController(
        IPdfImageExtractionService pdfImageExtraction,
        IBlobStorageService blobStorage,
        ILogger<PdfController> logger)
    {
        _pdfImageExtraction = pdfImageExtraction;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    // GET /Pdf
    public IActionResult Index() => View();

    /// <summary>
    /// POST /Pdf/ExtractImages
    /// Accepts a PDF upload and returns all embedded images as a ZIP archive.
    /// Returns 204 No Content if the PDF contains no images.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtractImages(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a PDF file.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only PDF files are supported.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var images = await _pdfImageExtraction.ExtractImagesAsync(stream);

            if (images.Count == 0)
            {
                TempData["Info"] = "No images found in the uploaded PDF.";
                return RedirectToAction(nameof(Index));
            }

            // Pack all extracted images into a ZIP and return it as a download
            var zipStream = BuildZip(images);
            var zipName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_images.zip";
            return File(zipStream, "application/zip", zipName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract images from PDF {FileName}", file.FileName);
            TempData["Error"] = $"Extraction failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// POST /Pdf/StoreImages
    /// Extracts all images from the uploaded PDF and persists each one as a blob.
    /// Blobs are stored under the path "{pdfBaseName}/{fileName}" so images from
    /// the same PDF are grouped together.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StoreImages(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a PDF file.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only PDF files are supported.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var images = await _pdfImageExtraction.ExtractImagesAsync(stream);

            if (images.Count == 0)
            {
                TempData["Info"] = "No images found in the uploaded PDF.";
                return RedirectToAction(nameof(Index));
            }

            var pdfBaseName = Path.GetFileNameWithoutExtension(file.FileName);
            var uploadTasks = images.Select(image =>
            {
                var blobName = $"{pdfBaseName}/{image.FileName}";
                var imageStream = new MemoryStream(image.Data);
                return _blobStorage.UploadAsync(blobName, imageStream, image.ContentType);
            });

            await Task.WhenAll(uploadTasks);

            _logger.LogInformation(
                "Stored {Count} image(s) from PDF {FileName} to blob storage.",
                images.Count, file.FileName);

            TempData["Success"] = $"{images.Count} image(s) extracted from \"{file.FileName}\" and saved to blob storage.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store images from PDF {FileName}", file.FileName);
            TempData["Error"] = $"Failed to store images: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static MemoryStream BuildZip(IReadOnlyList<Models.ExtractedImage> images)
    {
        var ms = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var image in images)
            {
                var entry = archive.CreateEntry(image.FileName, System.IO.Compression.CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                entryStream.Write(image.Data);
            }
        }
        ms.Position = 0;
        return ms;
    }
}
