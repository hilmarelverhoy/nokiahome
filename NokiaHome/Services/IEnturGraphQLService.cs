using System.Threading.Tasks;

namespace NokiaHome.Services
{
    public interface IEnturGraphQLService
    {
        Task<string> ExecuteQueryAsync(string query, object? variables = null);
        Task<string> GetTripPatternsAsync(string fromCode, string toCode);
        Task<string> GetTripPatternsAsync(
            string? fromCode, string? fromName, double[]? fromCoordinates,
            string? toCode, string? toName, double[]? toCoordinates);
    }
}