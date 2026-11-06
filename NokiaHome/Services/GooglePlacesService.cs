using System.Text.Json;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Places;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleMapsSettings _settings;
        private readonly ILogger<GooglePlacesService> _logger;

        private const string NearbyUrl = "https://places.googleapis.com/v1/places:searchNearby";
        private const string DetailsBaseUrl = "https://places.googleapis.com/v1/places/";

        // ponytail: Pro SKU fields only — add Enterprise fields (rating, phone, etc.) when you need them
        private const string DefaultNearbyFieldMask =
            "places.id,places.displayName,places.formattedAddress,places.location," +
            "places.primaryType,places.types,places.googleMapsUri,places.businessStatus";

        private const string DefaultDetailsFieldMask =
            "id,displayName,formattedAddress,location,primaryType,types," +
            "googleMapsUri,businessStatus,regularOpeningHours,nationalPhoneNumber,websiteUri,rating,userRatingCount";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GooglePlacesService(
            HttpClient httpClient,
            IOptions<GoogleMapsSettings> settings,
            ILogger<GooglePlacesService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<NearbySearchResponse?> SearchNearbyAsync(NearbySearchRequest request, string? fieldMask = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, NearbyUrl)
            {
                Content = JsonContent.Create(request)
            };
            req.Headers.Add("X-Goog-Api-Key", _settings.Key);
            req.Headers.Add("X-Goog-FieldMask", string.IsNullOrWhiteSpace(fieldMask) ? DefaultNearbyFieldMask : fieldMask);

            try
            {
                var response = await _httpClient.SendAsync(req);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<NearbySearchResponse>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search nearby places at ({Lat},{Lng})",
                    request.LocationRestriction.Circle.Center.Latitude,
                    request.LocationRestriction.Circle.Center.Longitude);
                return null;
            }
        }

        public async Task<Place?> GetPlaceDetailsAsync(string placeId, string? fieldMask = null)
        {
            // placeId may arrive as raw ID or full resource name "places/XXX" — normalise to bare ID
            var bareId = placeId.StartsWith("places/") ? placeId["places/".Length..] : placeId;

            using var req = new HttpRequestMessage(HttpMethod.Get, DetailsBaseUrl + Uri.EscapeDataString(bareId));
            req.Headers.Add("X-Goog-Api-Key", _settings.Key);
            req.Headers.Add("X-Goog-FieldMask", string.IsNullOrWhiteSpace(fieldMask) ? DefaultDetailsFieldMask : fieldMask);

            try
            {
                var response = await _httpClient.SendAsync(req);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Place>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get place details for {PlaceId}", bareId);
                return null;
            }
        }
    }
}
