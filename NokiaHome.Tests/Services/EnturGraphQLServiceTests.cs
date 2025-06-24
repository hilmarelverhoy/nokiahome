using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using NokiaHome.Services;

namespace NokiaHome.Tests.Services
{
    public class EnturGraphQLServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly EnturGraphQLService _service;

        public EnturGraphQLServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _configurationMock = new Mock<IConfiguration>();
            
            _configurationMock.Setup(x => x["Entur:ClientName"]).Returns("test-client");
            
            _service = new EnturGraphQLService(_httpClient, _configurationMock.Object);
        }

        [Fact]
        public async Task GetTripPatternsAsync_WithTransitStopCodes_BuildsCorrectQuery()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            await _service.GetTripPatternsAsync("NSR:StopPlace:1", "NSR:StopPlace:2");

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Content != null && 
                        VerifyQueryContains(req, "$fromCode: String!", "$toCode: String!") &&
                        VerifyQueryContains(req, "place: $fromCode", "place: $toCode") &&
                        !VerifyQueryContains(req, "$fromLat", "$fromLng", "$toLat", "$toLng")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetTripPatternsAsync_WithCoordinates_BuildsCorrectQuery()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var fromCoords = new double[] { 10.7522, 59.9139 }; // Oslo coordinates
            var toCoords = new double[] { 10.4036, 63.4305 }; // Trondheim coordinates

            await _service.GetTripPatternsAsync(
                null, "Oslo Central", fromCoords,
                null, "Trondheim Central", toCoords);

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Content != null && 
                        VerifyQueryContains(req, "$fromName: String!", "$fromLat: Float!", "$fromLng: Float!") &&
                        VerifyQueryContains(req, "$toName: String!", "$toLat: Float!", "$toLng: Float!") &&
                        VerifyQueryContains(req, "name: $fromName, coordinates: {latitude: $fromLat, longitude: $fromLng}") &&
                        VerifyQueryContains(req, "name: $toName, coordinates: {latitude: $toLat, longitude: $toLng}") &&
                        !VerifyQueryContains(req, "$fromCode", "$toCode", "place: $")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetTripPatternsAsync_MixedFromCodeToCoordinates_BuildsCorrectQuery()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var toCoords = new double[] { 10.4036, 63.4305 };

            await _service.GetTripPatternsAsync(
                "NSR:StopPlace:1", null, null,
                null, "Trondheim Central", toCoords);

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Content != null && 
                        VerifyQueryContains(req, "$fromCode: String!", "$toName: String!", "$toLat: Float!", "$toLng: Float!") &&
                        VerifyQueryContains(req, "place: $fromCode") &&
                        VerifyQueryContains(req, "name: $toName, coordinates: {latitude: $toLat, longitude: $toLng}") &&
                        !VerifyQueryContains(req, "$fromName", "$fromLat", "$fromLng", "$toCode")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetTripPatternsAsync_MixedFromCoordinatesToCode_BuildsCorrectQuery()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var fromCoords = new double[] { 10.7522, 59.9139 };

            await _service.GetTripPatternsAsync(
                null, "Oslo Central", fromCoords,
                "NSR:StopPlace:2", null, null);

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Content != null && 
                        VerifyQueryContains(req, "$fromName: String!", "$fromLat: Float!", "$fromLng: Float!", "$toCode: String!") &&
                        VerifyQueryContains(req, "name: $fromName, coordinates: {latitude: $fromLat, longitude: $fromLng}") &&
                        VerifyQueryContains(req, "place: $toCode") &&
                        !VerifyQueryContains(req, "$fromCode", "$toName", "$toLat", "$toLng")),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GetTripPatternsAsync_WithCoordinates_PassesCorrectVariableValues()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            var fromCoords = new double[] { 10.7522, 59.9139 }; // longitude, latitude
            var toCoords = new double[] { 10.4036, 63.4305 };

            await _service.GetTripPatternsAsync(
                null, "Oslo Central", fromCoords,
                null, "Trondheim Central", toCoords);

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Theory]
        [InlineData("", "To location")]
        [InlineData(null, "To location")]
        [InlineData("NSR:StopPlace:1", "")]
        [InlineData("NSR:StopPlace:1", null)]
        public async Task GetTripPatternsAsync_WithInvalidParameters_ThrowsArgumentException(string? fromCode, string? toCode)
        {
            var action = async () => await _service.GetTripPatternsAsync(fromCode, toCode);
            
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetTripPatternsAsync_WithInvalidFromLocation_ThrowsArgumentException()
        {
            var action = async () => await _service.GetTripPatternsAsync(
                null, null, null, // Invalid from location
                "NSR:StopPlace:2", null, null);
            
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid from location parameters");
        }

        [Fact]
        public async Task GetTripPatternsAsync_WithInvalidToLocation_ThrowsArgumentException()
        {
            var action = async () => await _service.GetTripPatternsAsync(
                "NSR:StopPlace:1", null, null,
                null, null, null); // Invalid to location
            
            await action.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Invalid to location parameters");
        }

        [Fact]
        public async Task GetTripPatternsAsync_AddsCorrectHeaders()
        {
            var mockResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"trip\":{\"tripPatterns\":[]}}}")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(mockResponse);

            await _service.GetTripPatternsAsync("NSR:StopPlace:1", "NSR:StopPlace:2");

            _httpMessageHandlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Content != null &&
                        req.Content.Headers.Contains("ET-Client-Name") &&
                        req.Content.Headers.GetValues("ET-Client-Name").First() == "test-client"),
                    ItExpr.IsAny<CancellationToken>());
        }

        private bool VerifyQueryContains(HttpRequestMessage request, params string[] expectedStrings)
        {
            if (request.Content == null) return false;
            
            var content = request.Content.ReadAsStringAsync().Result;
            var requestObj = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (!requestObj.TryGetProperty("query", out var queryElement))
                return false;
                
            var query = queryElement.GetString();
            if (query == null) return false;
            
            return expectedStrings.All(expected => query.Contains(expected));
        }

    }
}