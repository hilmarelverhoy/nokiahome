using NokiaHome.Models.Directions;

namespace NokiaHome.Services
{
    public interface IGoogleMapsDirectionsService
    {
        /// <summary>
        /// Gets driving directions between two locations.
        /// Origin and destination can be addresses or "lat,lng" strings.
        /// </summary>
        Task<DirectionsResponse?> GetDirectionsAsync(string origin, string destination, string mode = "driving");
    }
}
