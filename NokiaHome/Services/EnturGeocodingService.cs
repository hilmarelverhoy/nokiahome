using System.Text.Json;
using NokiaHome.Models.Geocoding;

namespace NokiaHome.Services
{
    public class EnturGeocodingService : IEnturGeocodingService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.entur.io/geocoder/v1/autocomplete";

        public EnturGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeocodingResponse?> SearchAsync(string searchText, string language = "no")
        {
            var url = $"{BaseUrl}?text={Uri.EscapeDataString(searchText)}&lang={language}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<GeocodingResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}