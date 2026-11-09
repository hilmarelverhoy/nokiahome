using NokiaHome.Models;

namespace NokiaHome.Services
{
    public interface IPrintJobStore
    {
        Task CreateAsync(PrintJob job, byte[] payload);
        Task<IReadOnlyList<string>> ListIdsAsync();
        Task<(PrintJob Job, string ETag)?> GetAsync(string id);
        /// <summary>Conditional update; returns the new ETag, or null if the ETag no longer matches.</summary>
        Task<string?> TryUpdateAsync(PrintJob job, string etag);
        Task<byte[]> GetPayloadAsync(string id);
        Task DeleteAsync(string id);
    }
}
