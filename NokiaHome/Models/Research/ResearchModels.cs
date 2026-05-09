namespace NokiaHome.Models.Research;

/// <summary>A single web source planned by the AI and fetched + summarised.</summary>
public class ResearchSource
{
    /// <summary>The URL the AI decided to fetch.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Short description of why this URL was chosen.</summary>
    public string Rationale { get; set; } = string.Empty;

    /// <summary>Human-readable summary produced by the second AI call.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>True when the fetch and summarisation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Error message if Success is false.</summary>
    public string? Error { get; set; }
}

/// <summary>The full result returned after processing a research question.</summary>
public class ResearchResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>The original question (typed or transcribed).</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>Sources planned, fetched and summarised by the pipeline.</summary>
    public List<ResearchSource> Sources { get; set; } = [];

    /// <summary>A final one-paragraph answer synthesised from all sources.</summary>
    public string FinalAnswer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
