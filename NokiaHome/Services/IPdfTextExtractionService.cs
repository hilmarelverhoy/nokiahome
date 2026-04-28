using NokiaHome.Models;

namespace NokiaHome.Services;

public interface IPdfTextExtractionService
{
    /// <summary>
    /// Extracts document metadata and the full text of every page from the given PDF stream.
    /// </summary>
    Task<PdfDocumentInfo> ExtractAsync(Stream pdfStream, string fileName);
}
