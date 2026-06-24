using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Directions;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class DirectionsPlacesService : IDirectionsPlacesService
    {
        private const string BlobName = "directions/places.json";

        private readonly BlobContainerClient _container;
        private readonly ILogger<DirectionsPlacesService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public DirectionsPlacesService(IOptions<BlobStorageSettings> options, ILogger<DirectionsPlacesService> logger)
        {
            var settings = options.Value;
            var serviceClient = new BlobServiceClient(settings.ConnectionString);
            _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
            _logger = logger;
        }

        public async Task<IReadOnlyList<SavedPlace>> GetPlacesAsync()
        {
            var places = await LoadAsync();
            return places.OrderByDescending(p => p.UseCount).ThenBy(p => p.Name).ToList();
        }

        public async Task SavePlaceAsync(string name)
        {
            name = name.Trim();
            var places = await LoadAsync();
            if (places.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return; // already saved
            places.Add(new SavedPlace { Name = name });
            await SaveAsync(places);
        }

        public async Task IncrementUseCountAsync(string name)
        {
            name = name.Trim();
            var places = await LoadAsync();
            var place = places.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (place is null) return;
            place.UseCount++;
            await SaveAsync(places);
        }

        // ── private helpers ──────────────────────────────────────────────────

        private async Task<List<SavedPlace>> LoadAsync()
        {
            try
            {
                var blob = _container.GetBlobClient(BlobName);
                if (!await blob.ExistsAsync()) return [];
                var download = await blob.DownloadContentAsync();
                return JsonSerializer.Deserialize<List<SavedPlace>>(download.Value.Content, JsonOptions) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load directions places");
                return [];
            }
        }

        private async Task SaveAsync(List<SavedPlace> places)
        {
            var json = JsonSerializer.Serialize(places, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            var blob = _container.GetBlobClient(BlobName);
            await blob.UploadAsync(new BinaryData(bytes), new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
            });
        }
    }
}
