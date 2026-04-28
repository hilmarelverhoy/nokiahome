namespace NokiaHome.Models;

/// <summary>All extracted textual content and metadata for a PDF document.</summary>
public class PdfDocumentInfo
{
    public string FileName { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public PdfMetadata Metadata { get; init; } = new();
    public IReadOnlyList<PdfPageInfo> Pages { get; init; } = [];
}

public class PdfMetadata
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Keywords { get; init; }
    public string? Creator { get; init; }
    public string? Producer { get; init; }
    public string? CreationDate { get; init; }
    public string? ModifiedDate { get; init; }
}

public class PdfPageInfo
{
    public int PageNumber { get; init; }
    public double WidthPoints { get; init; }
    public double HeightPoints { get; init; }

    /// <summary>All text on the page, words joined in reading order.</summary>
    public string Text { get; init; } = string.Empty;

    public int ImageCount { get; init; }
}
