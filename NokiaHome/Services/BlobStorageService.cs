using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IOptions<BlobStorageSettings> options)
        {
            var settings = options.Value;
            var serviceClient = new BlobServiceClient(settings.ConnectionString);
            _containerClient = serviceClient.GetBlobContainerClient(settings.ContainerName);
        }

        public async Task<IReadOnlyList<BlobItem>> ListBlobsAsync()
        {
            var blobs = new List<BlobItem>();
            await foreach (var blob in _containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }
            return blobs;
        }

        public async Task UploadAsync(string blobName, Stream content, string contentType)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            });
        }

        public async Task<(Stream Content, string ContentType)> DownloadAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DownloadStreamingAsync();
            var contentType = response.Value.Details.ContentType ?? "application/octet-stream";
            return (response.Value.Content, contentType);
        }

        public async Task DeleteAsync(string blobName)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
