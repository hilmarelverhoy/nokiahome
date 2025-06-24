using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Moq;
using NokiaHome.Controllers;
using NokiaHome.Models.Geocoding;
using NokiaHome.Models.Trip;
using NokiaHome.Services;
using NokiaHome.Tests.TestData;

namespace NokiaHome.Tests.Controllers
{
    public class JourneyControllerTests
    {
        private readonly Mock<IEnturGeocodingService> _geocodingServiceMock;
        private readonly Mock<IEnturGraphQLService> _graphQLServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly JourneyController _controller;

        public JourneyControllerTests()
        {
            _geocodingServiceMock = new Mock<IEnturGeocodingService>();
            _graphQLServiceMock = new Mock<IEnturGraphQLService>();
            _configurationMock = new Mock<IConfiguration>();
            
            _controller = new JourneyController(
                _geocodingServiceMock.Object,
                _graphQLServiceMock.Object,
                _configurationMock.Object);
        }

        [Theory]
        [InlineData("10.753051,59.910357", 10.753051, 59.910357)]
        [InlineData("10.4,63.5", 10.4, 63.5)]
        [InlineData("-10.123,45.678", -10.123, 45.678)]
        public async Task GetTripWithCoordinates_ValidCoordinates_ParsesCorrectly(
            string coordinateString, double expectedLng, double expectedLat)
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            await _controller.GetTripWithCoordinates(
                null, "From Location", coordinateString,
                "NSR:StopPlace:123", null, null);

            _graphQLServiceMock.Verify(
                x => x.GetTripPatternsAsync(
                    null, "From Location", 
                    It.Is<double[]>(coords => 
                        Math.Abs(coords[0] - expectedLng) < 0.0001 &&
                        Math.Abs(coords[1] - expectedLat) < 0.0001),
                    "NSR:StopPlace:123", null, null),
                Times.Once);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("10.5")]
        [InlineData("10.5,abc")]
        [InlineData("")]
        [InlineData(null)]
        public async Task GetTripWithCoordinates_InvalidCoordinates_PassesNullToService(string? coordinateString)
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            await _controller.GetTripWithCoordinates(
                null, "From Location", coordinateString,
                "NSR:StopPlace:123", null, null);

            _graphQLServiceMock.Verify(
                x => x.GetTripPatternsAsync(
                    null, "From Location", null,
                    "NSR:StopPlace:123", null, null),
                Times.Once);
        }

        [Fact]
        public async Task GetTripWithCoordinates_BothCoordinates_ParsesBothCorrectly()
        {
            var fromCoords = "10.753051,59.910357";
            var toCoords = "10.399123,63.436279";
            
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            await _controller.GetTripWithCoordinates(
                null, "Oslo S", fromCoords,
                null, "Trondheim S", toCoords);

            _graphQLServiceMock.Verify(
                x => x.GetTripPatternsAsync(
                    null, "Oslo S", 
                    It.Is<double[]>(coords => 
                        Math.Abs(coords[0] - 10.753051) < 0.0001 &&
                        Math.Abs(coords[1] - 59.910357) < 0.0001),
                    null, "Trondheim S",
                    It.Is<double[]>(coords => 
                        Math.Abs(coords[0] - 10.399123) < 0.0001 &&
                        Math.Abs(coords[1] - 63.436279) < 0.0001)),
                Times.Once);
        }

        [Fact]
        public async Task GetTripWithCoordinates_ValidResponse_ReturnsViewWithData()
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            var result = await _controller.GetTripWithCoordinates(
                "NSR:StopPlace:59872", null, null,
                "NSR:StopPlace:59977", null, null);

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewName.Should().Be("GetTrip");
            viewResult.Model.Should().BeOfType<TripResponse>();
            
            var model = viewResult.Model as TripResponse;
            model!.Data!.Trip!.TripPatterns.Should().HaveCount(2);
            model.Data.Trip.TripPatterns[0].Duration.Should().Be(27000);
        }

        [Fact]
        public async Task GetTripWithCoordinates_EmptyResponse_SetsErrorViewBag()
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.EmptyResponse);

            var result = await _controller.GetTripWithCoordinates(
                "NSR:StopPlace:59872", null, null,
                "NSR:StopPlace:59977", null, null);

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["ErrorMessage"].Should().Be("No trip patterns found in response");
        }

        [Fact]
        public async Task GetTripWithCoordinates_JsonException_HandlesGracefully()
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync("invalid json");

            var result = await _controller.GetTripWithCoordinates(
                "NSR:StopPlace:59872", null, null,
                "NSR:StopPlace:59977", null, null);

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["ErrorMessage"].ToString().Should().StartWith("Failed to parse trip data:");
        }

        [Fact]
        public async Task SearchFrom_ValidSearchText_CallsGeocodingService()
        {
            var mockResponse = JsonSerializer.Deserialize<GeocodingResponse>(
                GeocodingResponses.OsloSResponse, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            _geocodingServiceMock
                .Setup(x => x.SearchAsync("oslo", "no"))
                .ReturnsAsync(mockResponse);

            var result = await _controller.SearchFrom("oslo");

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewName.Should().Be("SearchResults");
            viewResult.ViewData["SearchType"].Should().Be("from");
            
            _geocodingServiceMock.Verify(x => x.SearchAsync("oslo", "no"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task SearchFrom_EmptySearchText_RedirectsToFrom(string? searchText)
        {
            var result = await _controller.SearchFrom(searchText!);

            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("From");
            
            _geocodingServiceMock.Verify(
                x => x.SearchAsync(It.IsAny<string>(), It.IsAny<string>()), 
                Times.Never);
        }

        [Fact]
        public async Task TripDetailsWithCoordinates_ValidTripIndex_ReturnsSelectedTrip()
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            var result = await _controller.TripDetailsWithCoordinates(
                "NSR:StopPlace:59872", null, null,
                "NSR:StopPlace:59977", null, null, 1);

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewName.Should().Be("TripDetails");
            viewResult.ViewData["TripIndex"].Should().Be(2); // 1-based index
            
            var model = viewResult.Model as TripPattern;
            model!.Duration.Should().Be(28200); // Second trip pattern
        }

        [Fact]
        public async Task TripDetailsWithCoordinates_InvalidTripIndex_ReturnsError()
        {
            _graphQLServiceMock
                .Setup(x => x.GetTripPatternsAsync(
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<double[]?>()))
                .ReturnsAsync(GraphQLResponses.OsloToTrondheimResponse);

            var result = await _controller.TripDetailsWithCoordinates(
                "NSR:StopPlace:59872", null, null,
                "NSR:StopPlace:59977", null, null, 99);

            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["ErrorMessage"].Should().Be("Trip not found");
        }
    }
}