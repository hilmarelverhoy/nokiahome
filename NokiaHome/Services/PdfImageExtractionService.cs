using NokiaHome.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace NokiaHome.Services;

public class PdfImageExtractionService : IPdfImageExtractionService
{
    private readonly ILogger<PdfImageExtractionService> _logger;

    public PdfImageExtractionService(ILogger<PdfImageExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ExtractedImage>> ExtractImagesAsync(Stream pdfStream)
    {
        // PdfDocument.Open is synchronous; wrap in Task.Run so callers can await without blocking
        return Task.Run<IReadOnlyList<ExtractedImage>>(() =>
        {
            // PdfPig requires a seekable stream; buffer to memory if necessary
            Stream source = pdfStream.CanSeek ? pdfStream : CopyToMemory(pdfStream);

            using var document = PdfDocument.Open(source);

            var results = new List<ExtractedImage>();

            foreach (var page in document.GetPages())
            {
                var pageImages = page.GetImages().ToList();
                for (int i = 0; i < pageImages.Count; i++)
                {
                    var pdfImage = pageImages[i];
                    try
                    {
                        if (TryGetImageBytes(pdfImage, out var data, out var contentType))
                        {
                            results.Add(new ExtractedImage
                            {
                                PageNumber = page.Number,
                                ImageIndex = i,
                                Data = data,
                                ContentType = contentType
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Could not extract image {Index} on page {Page}; skipping.",
                            i, page.Number);
                    }
                }
            }

            return results;
        });
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static bool TryGetImageBytes(IPdfImage image, out byte[] data, out string contentType)
    {
        // Prefer a lossless PNG representation when available
        if (image.TryGetPng(out var pngBytes))
        {
            data = pngBytes;
            contentType = "image/png";
            return true;
        }

        // Fall back to raw bytes (may be JPEG, JBIG2, etc.)
        if (image.TryGetBytes(out var rawBytes))
        {
            data = rawBytes.ToArray();
            contentType = DetectContentType(data);
            return true;
        }

        data = [];
        contentType = string.Empty;
        return false;
    }

    /// <summary>Detect MIME type from the first few bytes (magic numbers).</summary>
    private static string DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return "image/png";

        if (data.Length >= 4 && data[0] == 0x49 && data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00)
            return "image/tiff";

        if (data.Length >= 4 && data[0] == 0x4D && data[1] == 0x4D && data[2] == 0x00 && data[3] == 0x2A)
            return "image/tiff";

        return "application/octet-stream";
    }

    private static MemoryStream CopyToMemory(Stream source)
    {
        var ms = new MemoryStream();
        source.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
