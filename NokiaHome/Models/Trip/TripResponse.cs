using System.Text.Json.Serialization;

namespace NokiaHome.Models.Trip
{
    public class TripResponse
    {
        [JsonPropertyName("data")]
        public TripData? Data { get; set; }
    }

    public class TripData
    {
        [JsonPropertyName("trip")]
        public Trip? Trip { get; set; }
    }

    public class Trip
    {
        [JsonPropertyName("tripPatterns")]
        public List<TripPattern>? TripPatterns { get; set; }
    }

    public class TripPattern
    {
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("streetDistance")]
        public double StreetDistance { get; set; }

        [JsonPropertyName("legs")]
        public List<Leg>? Legs { get; set; }

        [JsonPropertyName("aimedEndTime")]
        public DateTime? AimedEndTime { get; set; }

        [JsonPropertyName("walkTime")]
        public int WalkTime { get; set; }

        [JsonPropertyName("waitingTime")]
        public int WaitingTime { get; set; }

        [JsonPropertyName("walkDistance")]
        public double WalkDistance { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("directDuration")]
        public int DirectDuration { get; set; }

        public string FormattedDuration => $"{Duration / 60} min";
        public string FormattedWalkTime => $"{WalkTime / 60} min";
        public string FormattedWaitingTime => $"{WaitingTime / 60} min";
        public string FormattedDistance => $"{Distance / 1000:F1} km";
        public string FormattedWalkDistance => $"{WalkDistance:F0} m";
        public string FormattedDirectDuration => $"{DirectDuration / 60} min";
    }

    public class Leg
    {
        [JsonPropertyName("expectedStartTime")]
        public DateTime ExpectedStartTime { get; set; }

        [JsonPropertyName("expectedEndTime")]
        public DateTime ExpectedEndTime { get; set; }

        [JsonPropertyName("mode")]
        public string? Mode { get; set; }

        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("line")]
        public Line? Line { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("fromPlace")]
        public Place? FromPlace { get; set; }

        [JsonPropertyName("toPlace")]
        public Place? ToPlace { get; set; }

        [JsonPropertyName("fromEstimatedCall")]
        public EstimatedCall? FromEstimatedCall { get; set; }

        public string FormattedStartTime => ExpectedStartTime.ToString("HH:mm");
        public string FormattedEndTime => ExpectedEndTime.ToString("HH:mm");
        public string FormattedDistance => $"{Distance / 1000:F1} km";
        public string FormattedDuration => $"{Duration / 60} min";
    }

    public class Line
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("publicCode")]
        public string? PublicCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("presentation")]
        public Presentation? Presentation { get; set; }

        [JsonPropertyName("transportMode")]
        public string? TransportMode { get; set; }

        [JsonPropertyName("transportSubmode")]
        public string? TransportSubmode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("branding")]
        public Branding? Branding { get; set; }

        [JsonPropertyName("notices")]
        public List<Notice>? Notices { get; set; }

        [JsonPropertyName("quays")]
        public List<Quay>? Quays { get; set; }
    }

    public class Presentation
    {
        [JsonPropertyName("colour")]
        public string? Colour { get; set; }

        [JsonPropertyName("textColour")]
        public string? TextColour { get; set; }

        public string HexColour => !string.IsNullOrEmpty(Colour) ? $"#{Colour}" : "#007bff";
        public string HexTextColour => !string.IsNullOrEmpty(TextColour) ? $"#{TextColour}" : "#ffffff";
    }

    public class Branding
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("shortName")]
        public string? ShortName { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class Notice
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("publicCode")]
        public string? PublicCode { get; set; }
    }

    public class Place
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("vertexType")]
        public string? VertexType { get; set; }

        [JsonPropertyName("quay")]
        public Quay? Quay { get; set; }
    }

    public class Quay
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("stopType")]
        public string? StopType { get; set; }
    }

    public class EstimatedCall
    {
        [JsonPropertyName("actualArrivalTime")]
        public DateTime? ActualArrivalTime { get; set; }

        [JsonPropertyName("actualDepartureTime")]
        public DateTime? ActualDepartureTime { get; set; }

        [JsonPropertyName("aimedArrivalTime")]
        public DateTime? AimedArrivalTime { get; set; }

        [JsonPropertyName("aimedDepartureTime")]
        public DateTime? AimedDepartureTime { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("quay")]
        public Quay? Quay { get; set; }

        public string FormattedAimedArrival => AimedArrivalTime?.ToString("HH:mm") ?? "";
        public string FormattedAimedDeparture => AimedDepartureTime?.ToString("HH:mm") ?? "";
        public string FormattedActualArrival => ActualArrivalTime?.ToString("HH:mm") ?? "";
        public string FormattedActualDeparture => ActualDepartureTime?.ToString("HH:mm") ?? "";
    }
}