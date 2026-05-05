using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NokiaHome.Models.Trip;
using NokiaHome.Services;

namespace NokiaHome.Tests.Integration
{
    [Collection("Integration")]
    public class EnturApiIntegrationTests
    {
        private readonly EnturGraphQLService _service;
        // Use 10 AM Norway time to ensure transit service is available
        private readonly DateTime _testDateTime = DateTime.Today.AddHours(10);

        public EnturApiIntegrationTests()
        {
            var httpClient = new HttpClient();
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(x => x["Entur:ClientName"]).Returns("nokiahome-test");
            _service = new EnturGraphQLService(httpClient, configurationMock.Object);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTripPatternsAsync_WithValidStopCodes_ReturnsTripPatterns()
        {
            var result = await _service.GetTripPatternsAsync(
                "NSR:StopPlace:59872",  // Oslo S
                null, null,
                "NSR:StopPlace:59977",  // Trondheim S
                null, null,
                _testDateTime);

            result.Should().NotBeNullOrEmpty();

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();

            var firstPattern = tripResponse.Data.Trip.TripPatterns![0];
            firstPattern.Duration.Should().BeGreaterThan(0);
            firstPattern.Legs.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTripPatternsAsync_WithCoordinates_ReturnsTripPatterns()
        {
            var result = await _service.GetTripPatternsAsync(
                null, "Oslo S", new double[] { 10.7522, 59.9114 },
                null, "Trondheim S", new double[] { 10.3950, 63.4371 },
                _testDateTime);

            result.Should().NotBeNullOrEmpty();

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTripPatternsAsync_ReturnsLegsWithValidData()
        {
            var result = await _service.GetTripPatternsAsync(
                "NSR:StopPlace:59872",
                null, null,
                "NSR:StopPlace:59977",
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var tripPatterns = tripResponse!.Data!.Trip!.TripPatterns!;
            tripPatterns.Should().NotBeNullOrEmpty();

            var hasTransitLeg = false;
            foreach (var pattern in tripPatterns)
            {
                pattern.Legs.Should().NotBeNullOrEmpty();

                foreach (var leg in pattern.Legs!)
                {
                    leg.ExpectedStartTime.Should().BeAfter(DateTime.MinValue);
                    leg.ExpectedEndTime.Should().BeAfter(DateTime.MinValue);
                    leg.Mode.Should().NotBeNullOrEmpty();
                    leg.Distance.Should().BeGreaterThanOrEqualTo(0);

                    if (leg.Mode != "foot" && leg.Mode != "bicycle")
                    {
                        hasTransitLeg = true;
                        leg.Line.Should().NotBeNull();
                    }
                }
            }

            hasTransitLeg.Should().BeTrue("Expected at least one transit leg");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTripPatternsAsync_ShortRoute_ReturnsResults()
        {
            // Drammen stasjon to Oslo S - reliable short route
            var result = await _service.GetTripPatternsAsync(
                "NSR:StopPlace:2103",   // Drammen stasjon
                null, null,
                "NSR:StopPlace:59872",  // Oslo S
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse!.Data!.Trip!.TripPatterns.Should().NotBeNullOrEmpty();
            
            // Should have rail/bus options for this route
            var patterns = tripResponse.Data.Trip.TripPatterns!;
            patterns.Should().NotBeNullOrEmpty();
            patterns[0].Duration.Should().BeGreaterThan(0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetTripPatternsAsync_ReturnsLineDetails()
        {
            var result = await _service.GetTripPatternsAsync(
                "NSR:StopPlace:59872",
                null, null,
                "NSR:StopPlace:59977",
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var patterns = tripResponse!.Data!.Trip!.TripPatterns!;
            patterns.Should().NotBeNullOrEmpty();

            var pattern = patterns[0];
            var transitLeg = pattern.Legs!.FirstOrDefault(l => l.Mode != "foot");
            transitLeg.Should().NotBeNull();
            transitLeg!.Line.Should().NotBeNull();
            transitLeg.Line!.PublicCode.Should().NotBeNullOrEmpty();
            transitLeg.Line.TransportMode.Should().NotBeNullOrEmpty();
        }
    }
}
