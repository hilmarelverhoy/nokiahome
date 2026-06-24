using NokiaHome.Models.Directions;

namespace NokiaHome.Services
{
    public interface IDirectionsPlacesService
    {
        Task<IReadOnlyList<SavedPlace>> GetPlacesAsync();
        Task SavePlaceAsync(string name);
        Task IncrementUseCountAsync(string name);
    }
}
