using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;
using NokiaHome.Models.Geocoding;
using NokiaHome.Models.Trip;
using System.Text.Json;

namespace NokiaHome.Controllers
{
    public class JourneyController : Controller
    {
        private readonly IEnturGeocodingService _geocodingService;
        private readonly IEnturGraphQLService _graphQLService;
        private readonly IConfiguration _configuration;

        public JourneyController(IEnturGeocodingService geocodingService, IEnturGraphQLService graphQLService, IConfiguration configuration)
        {
            _geocodingService = geocodingService;
            _graphQLService = graphQLService;
            _configuration = configuration;
        }

        public IActionResult From()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchFrom(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return RedirectToAction("From");
            }

            var results = await _geocodingService.SearchAsync(searchText);
            ViewBag.SearchType = "from";
            return View("SearchResults", results);
        }

        public IActionResult To(string? fromCode, string? fromName, string? fromCoordinates)
        {
            ViewBag.FromCode = fromCode;
            ViewBag.FromName = fromName;
            ViewBag.FromCoordinates = fromCoordinates;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchTo(string searchText, string? fromCode, string? fromName, string? fromCoordinates)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return RedirectToAction("To", new { fromCode, fromName, fromCoordinates });
            }

            var results = await _geocodingService.SearchAsync(searchText);
            ViewBag.SearchType = "to";
            ViewBag.FromCode = fromCode;
            ViewBag.FromName = fromName;
            ViewBag.FromCoordinates = fromCoordinates;
            return View("SearchResults", results);
        }

        public async Task<IActionResult> GetTrip(string fromCode, string toCode)
        {
            return await GetTripWithCoordinates(fromCode, null, null, toCode, null, null);
        }

        public async Task<IActionResult> GetTripWithCoordinates(
            string? fromCode, string? fromName, string? fromCoordinates,
            string? toCode, string? toName, string? toCoordinates)
        {
            try
            {
                // Parse coordinates if provided
                double[]? fromCoords = null;
                double[]? toCoords = null;

                if (!string.IsNullOrEmpty(fromCoordinates))
                {
                    var parts = fromCoordinates.Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[0], out var lng) && double.TryParse(parts[1], out var lat))
                    {
                        fromCoords = new double[] { lng, lat };
                    }
                }

                if (!string.IsNullOrEmpty(toCoordinates))
                {
                    var parts = toCoordinates.Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[0], out var lng) && double.TryParse(parts[1], out var lat))
                    {
                        toCoords = new double[] { lng, lat };
                    }
                }

                var jsonResult = await _graphQLService.GetTripPatternsAsync(
                    fromCode, fromName, fromCoords,
                    toCode, toName, toCoords);
                
                // Debug: Log the raw response
                Console.WriteLine($"GraphQL Response: {jsonResult}");
                
                var tripResponse = JsonSerializer.Deserialize<TripResponse>(jsonResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                // Debug: Check if we have data
                if (tripResponse?.Data?.Trip?.TripPatterns == null || !tripResponse.Data.Trip.TripPatterns.Any())
                {
                    ViewBag.ErrorMessage = "No trip patterns found in response";
                    ViewBag.RawResponse = jsonResult;
                    ViewBag.FromCode = fromCode;
                    ViewBag.ToCode = toCode;
                }
                
                ViewBag.FromCode = fromCode;
                ViewBag.FromName = fromName;
                ViewBag.ToCode = toCode;
                ViewBag.ToName = toName;
                ViewBag.FromCoordinates = fromCoordinates;
                ViewBag.ToCoordinates = toCoordinates;
                return View("GetTrip", tripResponse);
            }
            catch (JsonException ex)
            {
                ViewBag.ErrorMessage = $"Failed to parse trip data: {ex.Message}";
                return View("GetTrip");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error getting trip data: {ex.Message}";
                ViewBag.FromCode = fromCode ?? fromName;
                ViewBag.ToCode = toCode ?? toName;
                return View("GetTrip");
            }
        }

        public async Task<IActionResult> TripDetails(string fromCode, string toCode, int tripIndex)
        {
            return await TripDetailsWithCoordinates(fromCode, null, null, toCode, null, null, tripIndex);
        }

        public async Task<IActionResult> TripDetailsWithCoordinates(
            string? fromCode, string? fromName, string? fromCoordinates,
            string? toCode, string? toName, string? toCoordinates, int tripIndex)
        {
            try
            {
                // Parse coordinates if provided
                double[]? fromCoords = null;
                double[]? toCoords = null;

                if (!string.IsNullOrEmpty(fromCoordinates))
                {
                    var parts = fromCoordinates.Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[0], out var lng) && double.TryParse(parts[1], out var lat))
                    {
                        fromCoords = new double[] { lng, lat };
                    }
                }

                if (!string.IsNullOrEmpty(toCoordinates))
                {
                    var parts = toCoordinates.Split(',');
                    if (parts.Length == 2 && double.TryParse(parts[0], out var lng) && double.TryParse(parts[1], out var lat))
                    {
                        toCoords = new double[] { lng, lat };
                    }
                }

                var jsonResult = await _graphQLService.GetTripPatternsAsync(
                    fromCode, fromName, fromCoords,
                    toCode, toName, toCoords);
                
                var tripResponse = JsonSerializer.Deserialize<TripResponse>(jsonResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (tripResponse?.Data?.Trip?.TripPatterns != null && 
                    tripIndex >= 0 && 
                    tripIndex < tripResponse.Data.Trip.TripPatterns.Count)
                {
                    var selectedTrip = tripResponse.Data.Trip.TripPatterns[tripIndex];
                    ViewBag.TripIndex = tripIndex + 1;
                    ViewBag.FromCode = fromCode ?? fromName;
                    ViewBag.ToCode = toCode ?? toName;
                    return View("TripDetails", selectedTrip);
                }
                else
                {
                    ViewBag.ErrorMessage = "Trip not found";
                    return View("TripDetails");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error getting trip details: {ex.Message}";
                return View("TripDetails");
            }
        }
    }
}