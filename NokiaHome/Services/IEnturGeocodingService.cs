using NokiaHome.Models.Geocoding;

namespace NokiaHome.Services
{
    public interface IEnturGeocodingService
    {
        Task<GeocodingResponse?> SearchAsync(string searchText, string language = "no");
    }
}