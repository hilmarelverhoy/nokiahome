using System.Text.Json;
using FluentAssertions;
using NokiaHome.Models.Trip;
using NokiaHome.Tests.TestData;

namespace NokiaHome.Tests.Contract
{
    public class TripResponseContractTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void Deserialize_FullResponseWithAllFields_ParsesCorrectly()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.FullResponseWithAllFields, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data!.Trip.Should().NotBeNull();
            result.Data.Trip!.TripPatterns.Should().HaveCount(1);

            var pattern = result.Data.Trip.TripPatterns![0];
            pattern.Duration.Should().Be(13500);
            pattern.StreetDistance.Should().Be(450);
            pattern.WalkTime.Should().Be(300);
            pattern.WaitingTime.Should().Be(120);
            pattern.WalkDistance.Should().Be(450.5);
            pattern.Distance.Should().Be(15000);
            pattern.DirectDuration.Should().Be(12000);

            pattern.Legs.Should().HaveCount(3);

            var walkingLeg = pattern.Legs![0];
            walkingLeg.Mode.Should().Be("foot");
            walkingLeg.Line.Should().BeNull();
            walkingLeg.Distance.Should().Be(450.5);

            var railLeg = pattern.Legs[1];
            railLeg.Mode.Should().Be("rail");
            railLeg.Line.Should().NotBeNull();
            railLeg.Line!.Id.Should().Be("RUT:Line:1");
            railLeg.Line.PublicCode.Should().Be("L1");
            railLeg.Line.Name.Should().Be("L1 Skoyen - Lillestrom");
            railLeg.Line.TransportMode.Should().Be("rail");
            railLeg.Line.TransportSubmode.Should().Be("localRail");
            railLeg.Line.Presentation.Should().NotBeNull();
            railLeg.Line.Presentation!.Colour.Should().Be("0066CC");
            railLeg.Line.Presentation.TextColour.Should().Be("FFFFFF");
            railLeg.Line.Presentation.HexColour.Should().Be("#0066CC");
            railLeg.Line.Presentation.HexTextColour.Should().Be("#FFFFFF");

            railLeg.Line.Branding.Should().NotBeNull();
            railLeg.Line.Branding!.Name.Should().Be("Ruter");
            railLeg.Line.Branding.ShortName.Should().Be("Ruter");
            railLeg.Line.Branding.Url.Should().Be("https://ruter.no");

            railLeg.Line.Notices.Should().HaveCount(1);
            railLeg.Line.Notices![0].Text.Should().Be("Wheelchair accessible");
            railLeg.Line.Notices[0].PublicCode.Should().Be("WC");

            railLeg.Line.Quays.Should().HaveCount(1);
            railLeg.Line.Quays![0].Name.Should().Be("Platform 1");
            railLeg.Line.Quays[0].StopType.Should().Be("railStation");

            railLeg.FromPlace.Should().NotBeNull();
            railLeg.FromPlace!.Quay.Should().NotBeNull();
            railLeg.FromPlace.Quay!.Name.Should().Be("Spor 1");

            railLeg.FromEstimatedCall.Should().NotBeNull();
            railLeg.FromEstimatedCall!.ActualDepartureTime.Should().NotBeNull();
            railLeg.FromEstimatedCall.AimedDepartureTime.Should().NotBeNull();

            var estimatedCall = railLeg.FromEstimatedCall;
            estimatedCall.Quay.Should().NotBeNull();
            estimatedCall.Quay!.Name.Should().Be("Spor 1");
        }

        [Fact]
        public void Deserialize_ResponseWithOnlyRequiredFields_ParsesCorrectly()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.ResponseWithOnlyRequiredFields, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data!.Trip!.TripPatterns.Should().HaveCount(1);

            var pattern = result.Data.Trip.TripPatterns![0];
            pattern.Duration.Should().Be(3600);
            pattern.Legs.Should().HaveCount(1);

            var leg = pattern.Legs![0];
            leg.Mode.Should().Be("bus");
            leg.Distance.Should().Be(5000);
            leg.Line.Should().BeNull();
            leg.FromPlace.Should().BeNull();
            leg.ToPlace.Should().BeNull();
            leg.FromEstimatedCall.Should().BeNull();
        }

        [Fact]
        public void Deserialize_ResponseWithGraphQLErrors_HasErrors()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.ResponseWithGraphQLErrors, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data.Should().BeNull();
        }

        [Fact]
        public void Deserialize_ResponseWithNullTrip_HasNullTrip()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.ResponseWithNullTrip, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Data!.Trip.Should().BeNull();
        }

        [Fact]
        public void Deserialize_ResponseWithMultipleTripPatterns_ParsesAllPatterns()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.ResponseWithMultipleTripPatterns, _jsonOptions);

            result.Should().NotBeNull();
            var patterns = result!.Data!.Trip!.TripPatterns!;
            patterns.Should().HaveCount(3);

            patterns[0].Duration.Should().Be(3600);
            patterns[0].Legs![0].Line!.PublicCode.Should().Be("F1");

            patterns[1].Duration.Should().Be(4200);
            patterns[1].Legs![0].Line!.PublicCode.Should().Be("F2");

            patterns[2].Duration.Should().Be(5400);
            patterns[2].Legs![0].Mode.Should().Be("bus");
            patterns[2].Legs[0].Line!.PublicCode.Should().Be("100");
        }

        [Fact]
        public void Deserialize_ResponseWithWaterTransport_ParsesWaterMode()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.ResponseWithWaterTransport, _jsonOptions);

            var pattern = result!.Data!.Trip!.TripPatterns![0];
            var leg = pattern.Legs![0];

            leg.Mode.Should().Be("water");
            leg.Line!.TransportMode.Should().Be("water");
            leg.Line.TransportSubmode.Should().Be("localPassengerFerry");
            leg.Line.PublicCode.Should().Be("B1");
            leg.FromPlace!.Name.Should().Be("Aker Brygge");
            leg.ToPlace!.Name.Should().Be("Nesodden");
        }

        [Fact]
        public void TripPattern_FormattedProperties_ReturnCorrectValues()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.FullResponseWithAllFields, _jsonOptions);
            var pattern = result!.Data!.Trip!.TripPatterns![0];

            pattern.FormattedDuration.Should().Be("225 min");
            pattern.FormattedWalkTime.Should().Be("5 min");
            pattern.FormattedWaitingTime.Should().Be("2 min");
            pattern.FormattedDistance.Should().Be("15.0 km");
            pattern.FormattedWalkDistance.Should().Be("450 m");
            pattern.FormattedDirectDuration.Should().Be("200 min");
        }

        [Fact]
        public void Leg_FormattedProperties_ReturnCorrectValues()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLContractResponses.FullResponseWithAllFields, _jsonOptions);
            var leg = result!.Data!.Trip!.TripPatterns![0].Legs![1];

            leg.FormattedStartTime.Should().Be("14:00");
            leg.FormattedEndTime.Should().Be("14:25");
            leg.FormattedDuration.Should().Be("25 min");
            leg.FormattedDistance.Should().Be("14.5 km");
        }

        [Fact]
        public void Deserialize_ExistingOsloToTrondheimResponse_StillWorks()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLResponses.OsloToTrondheimResponse, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data!.Trip!.TripPatterns.Should().HaveCount(2);

            var firstPattern = result.Data.Trip.TripPatterns![0];
            firstPattern.Duration.Should().Be(27000);
            firstPattern.Legs.Should().HaveCount(3);

            var railLeg = firstPattern.Legs![0];
            railLeg.Mode.Should().Be("rail");
            railLeg.Line!.PublicCode.Should().Be("FLY2");
            railLeg.FromPlace!.Name.Should().Be("Oslo S");
        }

        [Fact]
        public void Deserialize_EmptyResponse_HasEmptyTripPatterns()
        {
            var result = JsonSerializer.Deserialize<TripResponse>(GraphQLResponses.EmptyResponse, _jsonOptions);

            result.Should().NotBeNull();
            result!.Data!.Trip!.TripPatterns.Should().BeEmpty();
        }
    }
}
