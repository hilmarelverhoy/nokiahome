using NokiaHome.Models.Research;

namespace NokiaHome.Services;

public interface IResearchService
{
    /// <summary>
    /// Runs the full research pipeline:
    ///   1. Transcribes audio (if provided) or uses the supplied text.
    ///   2. Calls GPT to plan a set of web requests.
    ///   3. Fetches each URL.
    ///   4. Calls GPT to summarise each page.
    ///   5. Calls GPT for a final synthesised answer.
    /// </summary>
    Task<ResearchResult> ResearchAsync(string? text, Stream? audio, string? audioFileName);
}
