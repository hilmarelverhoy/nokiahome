namespace NokiaHome.Tests.TestData
{
    public static class GeocodingResponses
    {
        public const string OsloSResponse = @"{
            ""type"": ""FeatureCollection"",
            ""features"": [
                {
                    ""type"": ""Feature"",
                    ""geometry"": {
                        ""type"": ""Point"",
                        ""coordinates"": [10.753051, 59.910357]
                    },
                    ""properties"": {
                        ""id"": ""NSR:StopPlace:59872"",
                        ""name"": ""Oslo S"",
                        ""label"": ""Oslo S, Oslo"",
                        ""category"": [""onstreetBus"", ""railStation"", ""onstreetBus""]
                    }
                }
            ]
        }";

        public const string TrondheimSResponse = @"{
            ""type"": ""FeatureCollection"",
            ""features"": [
                {
                    ""type"": ""Feature"",
                    ""geometry"": {
                        ""type"": ""Point"",
                        ""coordinates"": [10.399123, 63.436279]
                    },
                    ""properties"": {
                        ""id"": ""NSR:StopPlace:59977"",
                        ""name"": ""Trondheim S"",
                        ""label"": ""Trondheim S, Trondheim"",
                        ""category"": [""busStation"", ""railStation"", ""onstreetBus""]
                    }
                }
            ]
        }";

        public const string FredensborgveienResponse = @"{
            ""type"": ""FeatureCollection"",
            ""features"": [
                {
                    ""type"": ""Feature"",
                    ""geometry"": {
                        ""type"": ""Point"",
                        ""coordinates"": [10.746254205613143, 59.91777364244695]
                    },
                    ""properties"": {
                        ""id"": ""285691179"",
                        ""name"": ""Fredensborgveien 6A"",
                        ""label"": ""Fredensborgveien 6A, Oslo"",
                        ""category"": [""vegadresse""],
                        ""housenumber"": ""6A"",
                        ""street"": ""Fredensborgveien""
                    }
                }
            ]
        }";

        public const string EmptyResponse = @"{
            ""type"": ""FeatureCollection"",
            ""features"": []
        }";
    }
}