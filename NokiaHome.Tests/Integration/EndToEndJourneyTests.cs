using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NokiaHome.Models.Geocoding;
using NokiaHome.Models.Trip;
using NokiaHome.Services;

namespace NokiaHome.Tests.Integration
{
    [Collection("Integration")]
    public class EndToEndJourneyTests
    {
        private readonly EnturGeocodingService _geocodingService;
        private readonly EnturGraphQLService _graphQLService;
        private readonly DateTime _testDateTime = DateTime.Today.AddHours(10);

        public EndToEndJourneyTests()
        {
            var geocodingClient = new HttpClient();
            var graphQLClient = new HttpClient();
            
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(x => x["Entur:ClientName"]).Returns("nokiahome-e2e");
            
            _geocodingService = new EnturGeocodingService(geocodingClient);
            _graphQLService = new EnturGraphQLService(graphQLClient, configurationMock.Object);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "E2E")]
        public async Task AddressToStopPlace_SearchAddressAndGetJourney_ReturnsValidTrip()
        {
            // Step 1: Search for a known address (Fredensborgveien 6A, Oslo)
            var geocodingResult = await _geocodingService.SearchAsync("Fredensborgveien 6A, Oslo");

            geocodingResult.Should().NotBeNull();
            geocodingResult!.Features.Should().NotBeNullOrEmpty();

            var location = geocodingResult.Features[0];
            var coords = location.Geometry!.Coordinates;
            var name = location.Properties!.Label;

            coords.Should().HaveCount(2);
            coords[0].Should().BeGreaterThan(10.0); // longitude
            coords[1].Should().BeGreaterThan(59.0); // latitude

            // Step 2: Get journey from address coordinates to Trondheim S
            var tripResult = await _graphQLService.GetTripPatternsAsync(
                null, name, new double[] { coords[0], coords[1] },
                "NSR:StopPlace:59977",  // Trondheim S
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(tripResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();

            var pattern = tripResponse.Data.Trip.TripPatterns![0];
            pattern.Duration.Should().BeGreaterThan(0);
            pattern.Legs.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "E2E")]
        public async Task AddressToAddress_SearchTwoAddressesAndGetJourney_ReturnsValidTrip()
        {
            // Step 1: Search for origin address
            var fromResult = await _geocodingService.SearchAsync("Karl Johans gate 22, Oslo");

            fromResult.Should().NotBeNull();
            fromResult!.Features.Should().NotBeNullOrEmpty();

            var fromLocation = fromResult.Features[0];
            var fromCoords = fromLocation.Geometry!.Coordinates;
            var fromName = fromLocation.Properties!.Label;

            // Step 2: Search for destination address
            var toResult = await _geocodingService.SearchAsync("Solsiden 1, Trondheim");

            toResult.Should().NotBeNull();
            toResult!.Features.Should().NotBeNullOrEmpty();

            var toLocation = toResult.Features[0];
            var toCoords = toLocation.Geometry!.Coordinates;
            var toName = toLocation.Properties!.Label;

            // Step 3: Get journey between the two addresses
            var tripResult = await _graphQLService.GetTripPatternsAsync(
                null, fromName, new double[] { fromCoords[0], fromCoords[1] },
                null, toName, new double[] { toCoords[0], toCoords[1] },
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(tripResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();

            var pattern = tripResponse.Data.Trip.TripPatterns![0];
            pattern.Duration.Should().BeGreaterThan(0);
            pattern.Legs.Should().NotBeNullOrEmpty();

            // Verify the first and last legs exist (may be walking legs)
            pattern.Legs.First().Mode.Should().NotBeNullOrEmpty();
            pattern.Legs.Last().Mode.Should().NotBeNullOrEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "E2E")]
        public async Task AddressToStopPlace_SearchRailStationAndGetJourney_ReturnsValidTrip()
        {
            // Search for Drammen stasjon (a real rail station)
            var geocodingResult = await _geocodingService.SearchAsync("Drammen stasjon");

            geocodingResult.Should().NotBeNull();
            geocodingResult!.Features.Should().NotBeNullOrEmpty();

            var location = geocodingResult.Features[0];
            var props = location.Properties!;
            
            // Use ID if it's a stop place, otherwise use coordinates
            string? fromCode = null;
            double[]? fromCoords = null;
            string? fromName = null;

            if (props.Id != null && props.Id.Contains("StopPlace"))
            {
                fromCode = props.Id;
            }
            else
            {
                fromCoords = location.Geometry!.Coordinates;
                fromName = props.Label;
            }

            var tripResult = await _graphQLService.GetTripPatternsAsync(
                fromCode, fromName, fromCoords,
                "NSR:StopPlace:59872",  // Oslo S
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(tripResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();

            // Drammen to Oslo should have a rail leg
            var patterns = tripResponse.Data.Trip.TripPatterns!;
            patterns.Any(p => p.Legs!.Any(l => l.Mode == "rail")).Should().BeTrue();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "E2E")]
        public async Task AddressToStopPlace_SearchAddressNotFound_HandlesGracefully()
        {
            // Search for a non-existent address
            var geocodingResult = await _geocodingService.SearchAsync("zzznonexistent12345xyz");

            geocodingResult.Should().NotBeNull();
            geocodingResult!.Features.Should().BeEmpty();
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "E2E")]
        public async Task AddressToStopPlace_SearchBusStopAndGetJourney_ReturnsValidTrip()
        {
            // Search for a bus stop
            var geocodingResult = await _geocodingService.SearchAsync("Nationaltheatret, Oslo");

            geocodingResult.Should().NotBeNull();
            geocodingResult!.Features.Should().NotBeNullOrEmpty();

            var location = geocodingResult.Features[0];
            var props = location.Properties!;
            
            // Use ID if it's a stop place, otherwise use coordinates
            string? fromCode = null;
            double[]? fromCoords = null;
            string? fromName = null;

            if (props.Id != null && props.Id.Contains("StopPlace"))
            {
                fromCode = props.Id;
            }
            else
            {
                fromCoords = location.Geometry!.Coordinates;
                fromName = props.Label;
            }

            var tripResult = await _graphQLService.GetTripPatternsAsync(
                fromCode, fromName, fromCoords,
                "NSR:StopPlace:59872",  // Oslo S
                null, null,
                _testDateTime);

            var tripResponse = JsonSerializer.Deserialize<TripResponse>(tripResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            tripResponse.Should().NotBeNull();
            tripResponse!.Data.Should().NotBeNull();
            tripResponse.Data!.Trip.Should().NotBeNull();
            tripResponse.Data.Trip!.TripPatterns.Should().NotBeNullOrEmpty();
        }
    }
}
