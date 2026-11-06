using System.Text.Json.Serialization;

namespace NokiaHome.Models.Places
{
    public class NearbySearchResponse
    {
        [JsonPropertyName("places")]
        public List<Place>? Places { get; set; }
    }

    public class Place
    {
        // IDs Only SKU
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        // Essentials SKU
        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("shortFormattedAddress")]
        public string? ShortFormattedAddress { get; set; }

        [JsonPropertyName("addressComponents")]
        public List<AddressComponent>? AddressComponents { get; set; }

        [JsonPropertyName("plusCode")]
        public PlusCode? PlusCode { get; set; }

        [JsonPropertyName("location")]
        public LatLng? Location { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }

        // Pro SKU
        [JsonPropertyName("displayName")]
        public LocalizedText? DisplayName { get; set; }

        [JsonPropertyName("primaryType")]
        public string? PrimaryType { get; set; }

        [JsonPropertyName("primaryTypeDisplayName")]
        public LocalizedText? PrimaryTypeDisplayName { get; set; }

        [JsonPropertyName("businessStatus")]
        public string? BusinessStatus { get; set; }

        [JsonPropertyName("googleMapsUri")]
        public string? GoogleMapsUri { get; set; }

        [JsonPropertyName("iconBackgroundColor")]
        public string? IconBackgroundColor { get; set; }

        [JsonPropertyName("utcOffsetMinutes")]
        public int? UtcOffsetMinutes { get; set; }

        // Enterprise SKU
        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("userRatingCount")]
        public int? UserRatingCount { get; set; }

        [JsonPropertyName("priceLevel")]
        public string? PriceLevel { get; set; }

        [JsonPropertyName("websiteUri")]
        public string? WebsiteUri { get; set; }

        [JsonPropertyName("nationalPhoneNumber")]
        public string? NationalPhoneNumber { get; set; }

        [JsonPropertyName("internationalPhoneNumber")]
        public string? InternationalPhoneNumber { get; set; }

        [JsonPropertyName("currentOpeningHours")]
        public OpeningHours? CurrentOpeningHours { get; set; }

        [JsonPropertyName("regularOpeningHours")]
        public OpeningHours? RegularOpeningHours { get; set; }

        // Enterprise+ SKU
        [JsonPropertyName("editorialSummary")]
        public LocalizedText? EditorialSummary { get; set; }

        [JsonPropertyName("reviews")]
        public List<Review>? Reviews { get; set; }

        [JsonPropertyName("takeout")]
        public bool? Takeout { get; set; }

        [JsonPropertyName("delivery")]
        public bool? Delivery { get; set; }

        [JsonPropertyName("dineIn")]
        public bool? DineIn { get; set; }

        [JsonPropertyName("curbsidePickup")]
        public bool? CurbsidePickup { get; set; }

        [JsonPropertyName("reservable")]
        public bool? Reservable { get; set; }

        [JsonPropertyName("outdoorSeating")]
        public bool? OutdoorSeating { get; set; }

        [JsonPropertyName("liveMusic")]
        public bool? LiveMusic { get; set; }

        [JsonPropertyName("goodForChildren")]
        public bool? GoodForChildren { get; set; }

        [JsonPropertyName("goodForGroups")]
        public bool? GoodForGroups { get; set; }

        [JsonPropertyName("restroom")]
        public bool? Restroom { get; set; }

        [JsonPropertyName("servesBeer")]
        public bool? ServesBeer { get; set; }

        [JsonPropertyName("servesWine")]
        public bool? ServesWine { get; set; }

        [JsonPropertyName("servesBreakfast")]
        public bool? ServesBreakfast { get; set; }

        [JsonPropertyName("servesLunch")]
        public bool? ServesLunch { get; set; }

        [JsonPropertyName("servesDinner")]
        public bool? ServesDinner { get; set; }

        [JsonPropertyName("servesCoffee")]
        public bool? ServesCoffee { get; set; }

        [JsonPropertyName("servesCocktails")]
        public bool? ServesCocktails { get; set; }

        [JsonPropertyName("servesBrunch")]
        public bool? ServesBrunch { get; set; }

        [JsonPropertyName("servesDessert")]
        public bool? ServesDessert { get; set; }

        [JsonPropertyName("servesVegetarianFood")]
        public bool? ServesVegetarianFood { get; set; }

        [JsonPropertyName("allowsDogs")]
        public bool? AllowsDogs { get; set; }
    }

    public class LocalizedText
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }
    }

    public class AddressComponent
    {
        [JsonPropertyName("longText")]
        public string? LongText { get; set; }

        [JsonPropertyName("shortText")]
        public string? ShortText { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }

    public class PlusCode
    {
        [JsonPropertyName("globalCode")]
        public string? GlobalCode { get; set; }

        [JsonPropertyName("compoundCode")]
        public string? CompoundCode { get; set; }
    }

    public class OpeningHours
    {
        [JsonPropertyName("openNow")]
        public bool? OpenNow { get; set; }

        [JsonPropertyName("weekdayDescriptions")]
        public List<string>? WeekdayDescriptions { get; set; }

        [JsonPropertyName("periods")]
        public List<OpeningPeriod>? Periods { get; set; }
    }

    public class OpeningPeriod
    {
        [JsonPropertyName("open")]
        public OpeningPoint? Open { get; set; }

        [JsonPropertyName("close")]
        public OpeningPoint? Close { get; set; }
    }

    public class OpeningPoint
    {
        [JsonPropertyName("day")]
        public int? Day { get; set; }

        [JsonPropertyName("hour")]
        public int? Hour { get; set; }

        [JsonPropertyName("minute")]
        public int? Minute { get; set; }
    }

    public class Review
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("relativePublishTimeDescription")]
        public string? RelativePublishTimeDescription { get; set; }

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("text")]
        public LocalizedText? Text { get; set; }

        [JsonPropertyName("originalText")]
        public LocalizedText? OriginalText { get; set; }

        [JsonPropertyName("authorAttribution")]
        public AuthorAttribution? AuthorAttribution { get; set; }
    }

    public class AuthorAttribution
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("photoUri")]
        public string? PhotoUri { get; set; }
    }
}
