using Microsoft.AspNetCore.Mvc;
using NokiaHome.Models.Places;
using NokiaHome.Services;

namespace NokiaHome.Controllers
{
    public class PlacesController : Controller
    {
        private readonly IGooglePlacesService _placesService;
        private readonly ILogger<PlacesController> _logger;

        // Field mask options for Nearby Search (requires "places." prefix)
        public static readonly IReadOnlyList<(string Value, string Label, string Tier)> FieldOptions =
        [
            ("places.id",                    "ID",                      "Pro"),
            ("places.displayName",           "Display Name",            "Pro"),
            ("places.formattedAddress",      "Formatted Address",       "Pro"),
            ("places.location",              "Location (lat/lng)",      "Pro"),
            ("places.primaryType",           "Primary Type",            "Pro"),
            ("places.primaryTypeDisplayName","Primary Type Label",      "Pro"),
            ("places.types",                 "All Types",               "Pro"),
            ("places.businessStatus",        "Business Status",         "Pro"),
            ("places.googleMapsUri",         "Google Maps Link",        "Pro"),
            ("places.googleMapsLinks",       "Maps Deep Links",         "Pro"),
            ("places.iconBackgroundColor",   "Icon Color",              "Pro"),
            ("places.iconMaskBaseUri",       "Icon URI",                "Pro"),
            ("places.photos",                "Photos",                  "Pro"),
            ("places.plusCode",              "Plus Code",               "Pro"),
            ("places.shortFormattedAddress", "Short Address",           "Pro"),
            ("places.utcOffsetMinutes",      "UTC Offset",              "Pro"),
            ("places.viewport",              "Viewport",                "Pro"),
            ("places.rating",                "Rating",                  "Enterprise"),
            ("places.userRatingCount",       "Rating Count",            "Enterprise"),
            ("places.priceLevel",            "Price Level",             "Enterprise"),
            ("places.websiteUri",            "Website",                 "Enterprise"),
            ("places.internationalPhoneNumber","Intl Phone",            "Enterprise"),
            ("places.nationalPhoneNumber",   "National Phone",          "Enterprise"),
            ("places.currentOpeningHours",   "Current Hours",           "Enterprise"),
            ("places.regularOpeningHours",   "Regular Hours",           "Enterprise"),
            ("places.editorialSummary",      "Editorial Summary",       "Enterprise+"),
            ("places.reviews",               "Reviews",                 "Enterprise+"),
            ("places.servesBeer",            "Serves Beer",             "Enterprise+"),
            ("places.servesBreakfast",       "Serves Breakfast",        "Enterprise+"),
            ("places.servesCoffee",          "Serves Coffee",           "Enterprise+"),
            ("places.servesDinner",          "Serves Dinner",           "Enterprise+"),
            ("places.servesLunch",           "Serves Lunch",            "Enterprise+"),
            ("places.delivery",              "Delivery",                "Enterprise+"),
            ("places.dineIn",                "Dine-in",                 "Enterprise+"),
            ("places.takeout",               "Takeout",                 "Enterprise+"),
            ("places.reservable",            "Reservable",              "Enterprise+"),
            ("places.outdoorSeating",        "Outdoor Seating",         "Enterprise+"),
            ("places.parkingOptions",        "Parking",                 "Enterprise+"),
            ("places.paymentOptions",        "Payment Options",         "Enterprise+"),
            ("places.evChargeOptions",       "EV Charging",             "Enterprise+"),
            ("places.goodForChildren",       "Good for Children",       "Enterprise+"),
            ("places.goodForGroups",         "Good for Groups",         "Enterprise+"),
            ("places.liveMusic",             "Live Music",              "Enterprise+"),
            ("places.restroom",              "Restroom",                "Enterprise+"),
        ];

        // Field mask options for Place Details (no "places." prefix — different API shape)
        public static readonly IReadOnlyList<(string Value, string Label, string Tier)> DetailsFieldOptions =
        [
            ("id",                       "ID",                      "IDs Only"),
            ("name",                     "Resource Name",           "IDs Only"),
            ("attributions",             "Attributions",            "IDs Only"),
            ("formattedAddress",         "Formatted Address",       "Essentials"),
            ("shortFormattedAddress",    "Short Address",           "Essentials"),
            ("addressComponents",        "Address Components",      "Essentials"),
            ("plusCode",                 "Plus Code",               "Essentials"),
            ("location",                 "Location (lat/lng)",      "Essentials"),
            ("types",                    "All Types",               "Essentials"),
            ("viewport",                 "Viewport",                "Essentials"),
            ("displayName",              "Display Name",            "Pro"),
            ("primaryType",              "Primary Type",            "Pro"),
            ("primaryTypeDisplayName",   "Primary Type Label",      "Pro"),
            ("businessStatus",           "Business Status",         "Pro"),
            ("googleMapsUri",            "Google Maps Link",        "Pro"),
            ("googleMapsLinks",          "Maps Deep Links",         "Pro"),
            ("iconBackgroundColor",      "Icon Color",              "Pro"),
            ("iconMaskBaseUri",          "Icon URI",                "Pro"),
            ("photos",                   "Photos",                  "Pro"),
            ("utcOffsetMinutes",         "UTC Offset",              "Pro"),
            ("timeZone",                 "Time Zone",               "Pro"),
            ("rating",                   "Rating",                  "Enterprise"),
            ("userRatingCount",          "Rating Count",            "Enterprise"),
            ("priceLevel",               "Price Level",             "Enterprise"),
            ("priceRange",               "Price Range",             "Enterprise"),
            ("websiteUri",               "Website",                 "Enterprise"),
            ("internationalPhoneNumber", "Intl Phone",              "Enterprise"),
            ("nationalPhoneNumber",      "National Phone",          "Enterprise"),
            ("currentOpeningHours",      "Current Hours",           "Enterprise"),
            ("regularOpeningHours",      "Regular Hours",           "Enterprise"),
            ("editorialSummary",         "Editorial Summary",       "Enterprise+"),
            ("reviews",                  "Reviews",                 "Enterprise+"),
            ("servesBeer",               "Serves Beer",             "Enterprise+"),
            ("servesBreakfast",          "Serves Breakfast",        "Enterprise+"),
            ("servesCoffee",             "Serves Coffee",           "Enterprise+"),
            ("servesDinner",             "Serves Dinner",           "Enterprise+"),
            ("servesLunch",              "Serves Lunch",            "Enterprise+"),
            ("delivery",                 "Delivery",                "Enterprise+"),
            ("dineIn",                   "Dine-in",                 "Enterprise+"),
            ("takeout",                  "Takeout",                 "Enterprise+"),
            ("reservable",               "Reservable",              "Enterprise+"),
            ("outdoorSeating",           "Outdoor Seating",         "Enterprise+"),
            ("parkingOptions",           "Parking",                 "Enterprise+"),
            ("paymentOptions",           "Payment Options",         "Enterprise+"),
            ("evChargeOptions",          "EV Charging",             "Enterprise+"),
            ("goodForChildren",          "Good for Children",       "Enterprise+"),
            ("goodForGroups",            "Good for Groups",         "Enterprise+"),
            ("liveMusic",                "Live Music",              "Enterprise+"),
            ("restroom",                 "Restroom",                "Enterprise+"),
            ("allowsDogs",               "Allows Dogs",             "Enterprise+"),
        ];

        public PlacesController(IGooglePlacesService placesService, ILogger<PlacesController> logger)
        {
            _placesService = placesService;
            _logger = logger;
        }

        // GET /Places
        public IActionResult Search()
        {
            ViewBag.FieldOptions = FieldOptions;
            return View();
        }

        // GET /Places/Results?lat=...&lng=...&radius=...&types=...&fields=...
        public async Task<IActionResult> Results(
            double lat, double lng, double radius,
            string? types, string? fields, int maxResults = 20)
        {
            if (lat == 0 && lng == 0)
                return RedirectToAction(nameof(Search));

            var includedTypes = types?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var request = new NearbySearchRequest
            {
                LocationRestriction = new LocationRestriction
                {
                    Circle = new Circle
                    {
                        Center = new LatLng { Latitude = lat, Longitude = lng },
                        Radius = Math.Clamp(radius, 1, 50000)
                    }
                },
                IncludedTypes = includedTypes is { Count: > 0 } ? includedTypes : null,
                MaxResultCount = Math.Clamp(maxResults, 1, 20)
            };

            ViewBag.Lat = lat;
            ViewBag.Lng = lng;
            ViewBag.Radius = radius;
            ViewBag.Types = types;
            ViewBag.Fields = fields;
            ViewBag.MaxResults = maxResults;
            ViewBag.FieldOptions = FieldOptions;

            try
            {
                var result = await _placesService.SearchNearbyAsync(request, string.IsNullOrWhiteSpace(fields) ? null : fields);

                if (result?.Places is null or { Count: 0 })
                    ViewBag.ErrorMessage = "No places found for the given parameters.";

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search nearby places at ({Lat},{Lng})", lat, lng);
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View();
            }
        }

        // GET /Places/Details?placeId=...&fields=...
        public async Task<IActionResult> Details(string placeId, string? fields)
        {
            if (string.IsNullOrWhiteSpace(placeId))
                return RedirectToAction(nameof(Search));

            ViewBag.PlaceId = placeId;
            ViewBag.Fields = fields;
            ViewBag.DetailsFieldOptions = DetailsFieldOptions;

            try
            {
                var place = await _placesService.GetPlaceDetailsAsync(placeId, string.IsNullOrWhiteSpace(fields) ? null : fields);

                if (place is null)
                    ViewBag.ErrorMessage = "Place not found or API returned no data.";

                return View(place);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get place details for {PlaceId}", placeId);
                ViewBag.ErrorMessage = $"Error: {ex.Message}";
                return View();
            }
        }
    }
}
