namespace NokiaHome.Models;

public class ExtractedImage
{
    /// <summary>Page number (1-based) the image was found on.</summary>
    public int PageNumber { get; init; }

    /// <summary>Zero-based index of this image within its page.</summary>
    public int ImageIndex { get; init; }

    /// <summary>Raw image bytes (PNG-encoded where conversion is possible, otherwise raw PDF image bytes).</summary>
    public byte[] Data { get; init; } = [];

    /// <summary>MIME type, e.g. "image/png" or "image/jpeg".</summary>
    public string ContentType { get; init; } = "image/png";

    /// <summary>Suggested filename for this image.</summary>
    public string FileName => $"page{PageNumber}_image{ImageIndex}.{Extension}";

    private string Extension => ContentType switch
    {
        "image/jpeg" => "jpg",
        "image/png"  => "png",
        "image/tiff" => "tiff",
        _            => "bin"
    };
}
