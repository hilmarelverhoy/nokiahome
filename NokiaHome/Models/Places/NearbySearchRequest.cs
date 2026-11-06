using System.Text.Json.Serialization;

namespace NokiaHome.Models.Places
{
    public class NearbySearchRequest
    {
        [JsonPropertyName("locationRestriction")]
        public LocationRestriction LocationRestriction { get; set; } = new();

        [JsonPropertyName("includedTypes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? IncludedTypes { get; set; }

        [JsonPropertyName("excludedTypes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ExcludedTypes { get; set; }

        [JsonPropertyName("includedPrimaryTypes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? IncludedPrimaryTypes { get; set; }

        [JsonPropertyName("excludedPrimaryTypes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ExcludedPrimaryTypes { get; set; }

        [JsonPropertyName("maxResultCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int MaxResultCount { get; set; }

        [JsonPropertyName("rankPreference")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RankPreference { get; set; }

        [JsonPropertyName("languageCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LanguageCode { get; set; }

        [JsonPropertyName("regionCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RegionCode { get; set; }
    }

    public class LocationRestriction
    {
        [JsonPropertyName("circle")]
        public Circle Circle { get; set; } = new();
    }

    public class Circle
    {
        [JsonPropertyName("center")]
        public LatLng Center { get; set; } = new();

        [JsonPropertyName("radius")]
        public double Radius { get; set; }
    }

    public class LatLng
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
