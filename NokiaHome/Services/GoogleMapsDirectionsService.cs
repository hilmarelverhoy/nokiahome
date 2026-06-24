using System.Text.Json;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Directions;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class GoogleMapsDirectionsService : IGoogleMapsDirectionsService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleMapsSettings _settings;
        private readonly ILogger<GoogleMapsDirectionsService> _logger;

        private const string BaseUrl = "https://maps.googleapis.com/maps/api/directions/json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GoogleMapsDirectionsService(
            HttpClient httpClient,
            IOptions<GoogleMapsSettings> settings,
            ILogger<GoogleMapsDirectionsService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<DirectionsResponse?> GetDirectionsAsync(
            string origin, string destination, string mode = "driving")
        {
            var url = $"{BaseUrl}?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&mode={mode}&key={_settings.Key}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DirectionsResponse>(json, JsonOptions);

                if (result?.Status != "OK")
                    _logger.LogWarning("Google Maps Directions returned status {Status}", result?.Status);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get directions from {Origin} to {Destination}", origin, destination);
                return null;
            }
        }
    }
}
