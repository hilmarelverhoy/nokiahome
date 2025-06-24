using System.Text.Json.Serialization;

namespace NokiaHome.Models.Geocoding
{
    public class GeocodingResponse
    {
        [JsonPropertyName("geocoding")]
        public GeocodingInfo? Geocoding { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("features")]
        public List<Feature>? Features { get; set; }

        [JsonPropertyName("bbox")]
        public double[]? Bbox { get; set; }
    }

    public class GeocodingInfo
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("attribution")]
        public string? Attribution { get; set; }

        [JsonPropertyName("query")]
        public Query? Query { get; set; }

        [JsonPropertyName("engine")]
        public Engine? Engine { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    public class Query
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("parser")]
        public string? Parser { get; set; }

        [JsonPropertyName("tokens")]
        public string[]? Tokens { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("layers")]
        public string[]? Layers { get; set; }

        [JsonPropertyName("sources")]
        public string[]? Sources { get; set; }

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("lang")]
        public Language? Lang { get; set; }

        [JsonPropertyName("querySize")]
        public int QuerySize { get; set; }
    }

    public class Language
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("iso6391")]
        public string? Iso6391 { get; set; }

        [JsonPropertyName("iso6393")]
        public string? Iso6393 { get; set; }

        [JsonPropertyName("defaulted")]
        public bool Defaulted { get; set; }
    }

    public class Engine
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    public class Feature
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("geometry")]
        public Geometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Properties? Properties { get; set; }
    }

    public class Geometry
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("coordinates")]
        public double[]? Coordinates { get; set; }
    }

    public class Properties
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("gid")]
        public string? Gid { get; set; }

        [JsonPropertyName("layer")]
        public string? Layer { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("accuracy")]
        public string? Accuracy { get; set; }

        [JsonPropertyName("country_a")]
        public string? CountryA { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("county_gid")]
        public string? CountyGid { get; set; }

        [JsonPropertyName("locality")]
        public string? Locality { get; set; }

        [JsonPropertyName("locality_gid")]
        public string? LocalityGid { get; set; }

        [JsonPropertyName("borough")]
        public string? Borough { get; set; }

        [JsonPropertyName("borough_gid")]
        public string? BoroughGid { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("category")]
        public string[]? Category { get; set; }

        [JsonPropertyName("tariff_zones")]
        public string[]? TariffZones { get; set; }
    }
}