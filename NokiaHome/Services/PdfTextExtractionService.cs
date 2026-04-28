using NokiaHome.Models;
using UglyToad.PdfPig;

namespace NokiaHome.Services;

public class PdfTextExtractionService : IPdfTextExtractionService
{
    private readonly ILogger<PdfTextExtractionService> _logger;

    public PdfTextExtractionService(ILogger<PdfTextExtractionService> logger)
    {
        _logger = logger;
    }

    public Task<PdfDocumentInfo> ExtractAsync(Stream pdfStream, string fileName)
    {
        return Task.Run<PdfDocumentInfo>(() =>
        {
            Stream source = pdfStream.CanSeek ? pdfStream : CopyToMemory(pdfStream);

            using var document = PdfDocument.Open(source);

            var info = document.Information;

            var metadata = new PdfMetadata
            {
                Title        = NullIfEmpty(info.Title),
                Author       = NullIfEmpty(info.Author),
                Subject      = NullIfEmpty(info.Subject),
                Keywords     = NullIfEmpty(info.Keywords),
                Creator      = NullIfEmpty(info.Creator),
                Producer     = NullIfEmpty(info.Producer),
                CreationDate = NullIfEmpty(info.CreationDate),
                ModifiedDate = NullIfEmpty(info.ModifiedDate),
            };

            var pages = new List<PdfPageInfo>();

            foreach (var page in document.GetPages())
            {
                string text;
                try
                {
                    // GetWords() gives reading-order word tokens; join with spaces and
                    // insert a newline between blocks that are spatially separated.
                    var words = page.GetWords().ToList();
                    text = string.Join(" ", words.Select(w => w.Text));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract text from page {Page}; using empty string.", page.Number);
                    text = string.Empty;
                }

                int imageCount;
                try { imageCount = page.GetImages().Count(); }
                catch { imageCount = 0; }

                pages.Add(new PdfPageInfo
                {
                    PageNumber   = page.Number,
                    WidthPoints  = page.Width,
                    HeightPoints = page.Height,
                    Text         = text,
                    ImageCount   = imageCount,
                });
            }

            return new PdfDocumentInfo
            {
                FileName  = fileName,
                PageCount = document.NumberOfPages,
                Metadata  = metadata,
                Pages     = pages,
            };
        });
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static MemoryStream CopyToMemory(Stream source)
    {
        var ms = new MemoryStream();
        source.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }
}
