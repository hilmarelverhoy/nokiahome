using System.Text.Json.Serialization;

namespace NokiaHome.Models.Directions
{
    public class DirectionsResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("routes")]
        public List<Route> Routes { get; set; } = [];
    }

    public class Route
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("legs")]
        public List<Leg> Legs { get; set; } = [];
    }

    public class Leg
    {
        [JsonPropertyName("distance")]
        public TextValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public TextValue? Duration { get; set; }

        [JsonPropertyName("start_address")]
        public string StartAddress { get; set; } = string.Empty;

        [JsonPropertyName("end_address")]
        public string EndAddress { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<Step> Steps { get; set; } = [];
    }

    public class Step
    {
        [JsonPropertyName("html_instructions")]
        public string HtmlInstructions { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public TextValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public TextValue? Duration { get; set; }

        [JsonPropertyName("travel_mode")]
        public string TravelMode { get; set; } = string.Empty;
    }

    public class TextValue
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
