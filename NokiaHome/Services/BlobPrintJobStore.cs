using System.Text.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NokiaHome.Models;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class BlobPrintJobStore : IPrintJobStore
    {
        private readonly BlobContainerClient _container;
        private bool _ensured;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public BlobPrintJobStore(IOptions<BlobStorageSettings> blob, IOptions<PrinterSettings> printer)
        {
            _container = new BlobServiceClient(blob.Value.ConnectionString)
                .GetBlobContainerClient(printer.Value.ContainerName);
        }

        private async Task EnsureAsync()
        {
            if (_ensured) return;
            await _gate.WaitAsync();
            try
            {
                if (!_ensured)
                {
                    await _container.CreateIfNotExistsAsync();
                    _ensured = true;
                }
            }
            finally { _gate.Release(); }
        }

        public async Task CreateAsync(PrintJob job, byte[] payload)
        {
            await EnsureAsync();
            await _container.GetBlobClient($"{job.Id}.bin").UploadAsync(new BinaryData(payload), overwrite: true);
            await _container.GetBlobClient($"{job.Id}.json")
                .UploadAsync(BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(job)), overwrite: true);
        }

        public async Task<IReadOnlyList<string>> ListIdsAsync()
        {
            await EnsureAsync();
            var ids = new List<string>();
            await foreach (var b in _container.GetBlobsAsync())
                if (b.Name.EndsWith(".json", StringComparison.Ordinal))
                    ids.Add(b.Name[..^5]);
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public async Task<(PrintJob Job, string ETag)?> GetAsync(string id)
        {
            await EnsureAsync();
            try
            {
                var r = await _container.GetBlobClient($"{id}.json").DownloadContentAsync();
                var job = r.Value.Content.ToObjectFromJson<PrintJob>();
                return job == null ? null : (job, r.Value.Details.ETag.ToString());
            }
            catch (RequestFailedException e) when (e.Status == 404) { return null; }
        }

        public async Task<string?> TryUpdateAsync(PrintJob job, string etag)
        {
            try
            {
                var r = await _container.GetBlobClient($"{job.Id}.json").UploadAsync(
                    BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(job)),
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { IfMatch = new ETag(etag) } });
                return r.Value.ETag.ToString();
            }
            catch (RequestFailedException e) when (e.Status is 412 or 404) { return null; }
        }

        public async Task<byte[]> GetPayloadAsync(string id)
        {
            var r = await _container.GetBlobClient($"{id}.bin").DownloadContentAsync();
            return r.Value.Content.ToArray();
        }

        public async Task DeleteAsync(string id)
        {
            await _container.GetBlobClient($"{id}.json").DeleteIfExistsAsync();
            await _container.GetBlobClient($"{id}.bin").DeleteIfExistsAsync();
        }
    }
}
