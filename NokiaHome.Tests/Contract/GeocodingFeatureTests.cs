using FluentAssertions;
using NokiaHome.Models.Geocoding;

namespace NokiaHome.Tests.Contract
{
    public class GeocodingFeatureTests
    {
        [Fact]
        public void IsValidForJourney_StopPlaceId_ReturnsTrue()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "NSR:StopPlace:59872", Name = "Oslo S" }
            };

            feature.IsValidForJourney.Should().BeTrue();
            feature.HasStopPlaceId.Should().BeTrue();
            feature.HasCoordinates.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_CoordinatesOnly_ReturnsTrue()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "address_123", Name = "Karl Johans gate 22" },
                Geometry = new Geometry { Coordinates = new double[] { 10.7522, 59.9114 } }
            };

            feature.IsValidForJourney.Should().BeTrue();
            feature.HasStopPlaceId.Should().BeFalse();
            feature.HasCoordinates.Should().BeTrue();
        }

        [Fact]
        public void IsValidForJourney_StopPlaceIdAndCoordinates_ReturnsTrue()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "NSR:StopPlace:59872", Name = "Oslo S" },
                Geometry = new Geometry { Coordinates = new double[] { 10.7522, 59.9114 } }
            };

            feature.IsValidForJourney.Should().BeTrue();
            feature.HasStopPlaceId.Should().BeTrue();
            feature.HasCoordinates.Should().BeTrue();
        }

        [Fact]
        public void IsValidForJourney_NoIdAndNoCoordinates_ReturnsFalse()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "some_raw_id", Name = "Unknown Location" }
            };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasStopPlaceId.Should().BeFalse();
            feature.HasCoordinates.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_NullIdAndNoCoordinates_ReturnsFalse()
        {
            var feature = new Feature
            {
                Properties = new Properties { Name = "No ID Location" }
            };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasStopPlaceId.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_NullProperties_ReturnsFalse()
        {
            var feature = new Feature { Properties = null };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasStopPlaceId.Should().BeFalse();
            feature.HasCoordinates.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_EmptyCoordinates_ReturnsFalse()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "address_123" },
                Geometry = new Geometry { Coordinates = Array.Empty<double>() }
            };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasCoordinates.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_SingleCoordinate_ReturnsFalse()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "address_123" },
                Geometry = new Geometry { Coordinates = new double[] { 10.7522 } }
            };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasCoordinates.Should().BeFalse();
        }

        [Fact]
        public void IsValidForJourney_NullGeometry_ReturnsFalse()
        {
            var feature = new Feature
            {
                Properties = new Properties { Id = "address_123" },
                Geometry = null
            };

            feature.IsValidForJourney.Should().BeFalse();
            feature.HasCoordinates.Should().BeFalse();
        }
    }
}
