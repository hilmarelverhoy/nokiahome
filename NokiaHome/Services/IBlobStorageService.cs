using Azure.Storage.Blobs.Models;

namespace NokiaHome.Services
{
    public interface IBlobStorageService
    {
        Task<IReadOnlyList<BlobItem>> ListBlobsAsync();
        Task UploadAsync(string blobName, Stream content, string contentType);
        Task<(Stream Content, string ContentType)> DownloadAsync(string blobName);
        Task DeleteAsync(string blobName);
    }
}
