using Microsoft.AspNetCore.Mvc;
using NokiaHome.Services;

namespace NokiaHome.Controllers
{
    public class DirectionsController : Controller
    {
        private readonly IGoogleMapsDirectionsService _directionsService;
        private readonly IDirectionsPlacesService _placesService;
        private readonly ILogger<DirectionsController> _logger;

        public DirectionsController(
            IGoogleMapsDirectionsService directionsService,
            IDirectionsPlacesService placesService,
            ILogger<DirectionsController> logger)
        {
            _directionsService = directionsService;
            _placesService = placesService;
            _logger = logger;
        }

        // GET /Directions
        public async Task<IActionResult> From()
        {
            ViewBag.SavedPlaces = await _placesService.GetPlacesAsync();
            return View();
        }

        // GET /Directions/To?origin=...
        public async Task<IActionResult> To(string origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
                return RedirectToAction(nameof(From));

            ViewBag.Origin = origin;
            ViewBag.SavedPlaces = await _placesService.GetPlacesAsync();
            return View();
        }

        // GET /Directions/Results?origin=...&destination=...&mode=...
        public async Task<IActionResult> Results(string origin, string destination, string mode = "driving")
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination))
                return RedirectToAction(nameof(From));

            ViewBag.Origin = origin;
            ViewBag.Destination = destination;
            ViewBag.Mode = mode;
            ViewBag.SavedPlaces = await _placesService.GetPlacesAsync();

            await Task.WhenAll(
                _placesService.IncrementUseCountAsync(origin),
                _placesService.IncrementUseCountAsync(destination)
            );

            try
            {
                var result = await _directionsService.GetDirectionsAsync(origin, destination, mode);

                if (result?.Status != "OK")
                    ViewBag.ErrorMessage = $"No directions found ({result?.Status ?? "no response"}).";

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get directions from {Origin} to {Destination}", origin, destination);
                ViewBag.ErrorMessage = $"Error getting directions: {ex.Message}";
                return View();
            }
        }

        // POST /Directions/SavePlace
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePlace(string name, string returnAction, string? returnOrigin)
        {
            if (!string.IsNullOrWhiteSpace(name))
                await _placesService.SavePlaceAsync(name);

            return returnAction == "To"
                ? RedirectToAction(nameof(To), new { origin = returnOrigin })
                : RedirectToAction(nameof(From));
        }
    }
}
