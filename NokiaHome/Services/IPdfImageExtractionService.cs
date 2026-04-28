using NokiaHome.Models;

namespace NokiaHome.Services;

public interface IPdfImageExtractionService
{
    /// <summary>
    /// Extracts all images embedded in the given PDF stream.
    /// Returns an empty list if the PDF contains no images.
    /// </summary>
    /// <param name="pdfStream">Readable stream containing the PDF data.</param>
    Task<IReadOnlyList<ExtractedImage>> ExtractImagesAsync(Stream pdfStream);
}
