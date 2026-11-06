using NokiaHome.Models.Places;

namespace NokiaHome.Services
{
    public interface IGooglePlacesService
    {
        Task<NearbySearchResponse?> SearchNearbyAsync(NearbySearchRequest request, string? fieldMask = null);
        Task<Place?> GetPlaceDetailsAsync(string placeId, string? fieldMask = null);
    }
}
