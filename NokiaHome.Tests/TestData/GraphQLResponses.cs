namespace NokiaHome.Tests.TestData
{
    public static class GraphQLResponses
    {
        public const string OsloToTrondheimResponse = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": [
                        {
                            ""duration"": 27000,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2025-06-22T23:10:00+02:00"",
                                    ""expectedEndTime"": ""2025-06-22T23:29:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""line"": {
                                        ""publicCode"": ""FLY2"",
                                        ""name"": ""FLY2""
                                    },
                                    ""fromPlace"": {
                                        ""name"": ""Oslo S""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Oslo lufthavn stasjon""
                                    }
                                },
                                {
                                    ""expectedStartTime"": ""2025-06-22T23:29:00+02:00"",
                                    ""expectedEndTime"": ""2025-06-22T23:30:50+02:00"",
                                    ""mode"": ""foot"",
                                    ""line"": null,
                                    ""fromPlace"": {
                                        ""name"": ""Oslo lufthavn stasjon""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Oslo lufthavn stasjon""
                                    }
                                },
                                {
                                    ""expectedStartTime"": ""2025-06-22T23:38:00+02:00"",
                                    ""expectedEndTime"": ""2025-06-23T06:40:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""line"": {
                                        ""publicCode"": ""F6"",
                                        ""name"": ""Dovrebanen""
                                    },
                                    ""fromPlace"": {
                                        ""name"": ""Oslo lufthavn stasjon""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Trondheim S""
                                    }
                                }
                            ]
                        },
                        {
                            ""duration"": 28200,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2025-06-22T22:50:00+02:00"",
                                    ""expectedEndTime"": ""2025-06-23T06:40:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""line"": {
                                        ""publicCode"": ""F6"",
                                        ""name"": ""Dovrebanen""
                                    },
                                    ""fromPlace"": {
                                        ""name"": ""Oslo S""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Trondheim S""
                                    }
                                }
                            ]
                        }
                    ]
                }
            }
        }";

        public const string EmptyResponse = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": []
                }
            }
        }";

        public const string ErrorResponse = @"{
            ""errors"": [
                {
                    ""message"": ""Validation error"",
                    ""locations"": [
                        {
                            ""line"": 2,
                            ""column"": 3
                        }
                    ]
                }
            ]
        }";
    }
}