namespace NokiaHome.Tests.TestData
{
    public static class GraphQLContractResponses
    {
        public const string FullResponseWithAllFields = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": [
                        {
                            ""duration"": 13500,
                            ""streetDistance"": 450,
                            ""walkTime"": 300,
                            ""waitingTime"": 120,
                            ""walkDistance"": 450.5,
                            ""distance"": 15000,
                            ""directDuration"": 12000,
                            ""expectedEndTime"": ""2026-05-01T14:30:00+02:00"",
                            ""aimedEndTime"": ""2026-05-01T14:28:00+02:00"",
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T13:45:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T13:55:00+02:00"",
                                    ""mode"": ""foot"",
                                    ""distance"": 450.5,
                                    ""duration"": 600,
                                    ""line"": null,
                                    ""fromPlace"": {
                                        ""name"": ""Oslo S"",
                                        ""vertexType"": ""NORMAL""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Jernbanetorget"",
                                        ""vertexType"": ""NORMAL""
                                    },
                                    ""fromEstimatedCall"": null
                                },
                                {
                                    ""expectedStartTime"": ""2026-05-01T14:00:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T14:25:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""distance"": 14500,
                                    ""duration"": 1500,
                                    ""line"": {
                                        ""id"": ""RUT:Line:1"",
                                        ""publicCode"": ""L1"",
                                        ""name"": ""L1 Skoyen - Lillestrom"",
                                        ""transportMode"": ""rail"",
                                        ""transportSubmode"": ""localRail"",
                                        ""description"": ""Local train line L1"",
                                        ""presentation"": {
                                            ""colour"": ""0066CC"",
                                            ""textColour"": ""FFFFFF""
                                        },
                                        ""branding"": {
                                            ""id"": ""RUT:Branding:1"",
                                            ""name"": ""Ruter"",
                                            ""shortName"": ""Ruter"",
                                            ""description"": ""Public transport in Oslo"",
                                            ""url"": ""https://ruter.no""
                                        },
                                        ""notices"": [
                                            {
                                                ""text"": ""Wheelchair accessible"",
                                                ""publicCode"": ""WC""
                                            }
                                        ],
                                        ""quays"": [
                                            {
                                                ""name"": ""Platform 1"",
                                                ""description"": ""Main platform"",
                                                ""stopType"": ""railStation""
                                            }
                                        ]
                                    },
                                    ""fromPlace"": {
                                        ""name"": ""Jernbanetorget"",
                                        ""vertexType"": ""NORMAL"",
                                        ""quay"": {
                                            ""name"": ""Spor 1"",
                                            ""description"": ""Track 1""
                                        }
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Trondheim S"",
                                        ""vertexType"": ""NORMAL""
                                    },
                                    ""fromEstimatedCall"": {
                                        ""actualArrivalTime"": ""2026-05-01T13:58:00+02:00"",
                                        ""actualDepartureTime"": ""2026-05-01T14:01:00+02:00"",
                                        ""aimedArrivalTime"": ""2026-05-01T13:59:00+02:00"",
                                        ""aimedDepartureTime"": ""2026-05-01T14:00:00+02:00"",
                                        ""date"": ""2026-05-01T00:00:00+02:00"",
                                        ""quay"": {
                                            ""name"": ""Spor 1"",
                                            ""description"": ""Track 1""
                                        }
                                    }
                                },
                                {
                                    ""expectedStartTime"": ""2026-05-01T14:25:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T14:30:00+02:00"",
                                    ""mode"": ""foot"",
                                    ""distance"": 200,
                                    ""duration"": 300,
                                    ""line"": null,
                                    ""fromPlace"": {
                                        ""name"": ""Trondheim S"",
                                        ""vertexType"": ""NORMAL""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Destination"",
                                        ""vertexType"": ""NORMAL""
                                    },
                                    ""fromEstimatedCall"": null
                                }
                            ]
                        }
                    ]
                }
            }
        }";

        public const string ResponseWithOnlyRequiredFields = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": [
                        {
                            ""duration"": 3600,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T10:00:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T11:00:00+02:00"",
                                    ""mode"": ""bus"",
                                    ""distance"": 5000
                                }
                            ]
                        }
                    ]
                }
            }
        }";

        public const string ResponseWithGraphQLErrors = @"{
            ""errors"": [
                {
                    ""message"": ""Variable '$fromCode' of type 'String!' is required but was not provided."",
                    ""locations"": [
                        {
                            ""line"": 2,
                            ""column"": 3
                        }
                    ],
                    ""extensions"": {
                        ""classification"": ""ValidationError""
                    }
                }
            ],
            ""data"": null
        }";

        public const string ResponseWithNullTrip = @"{
            ""data"": {
                ""trip"": null
            }
        }";

        public const string ResponseWithMultipleTripPatterns = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": [
                        {
                            ""duration"": 3600,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T10:00:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T11:00:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""distance"": 50000,
                                    ""line"": {
                                        ""publicCode"": ""F1"",
                                        ""transportMode"": ""rail""
                                    }
                                }
                            ]
                        },
                        {
                            ""duration"": 4200,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T10:15:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T11:30:00+02:00"",
                                    ""mode"": ""rail"",
                                    ""distance"": 50000,
                                    ""line"": {
                                        ""publicCode"": ""F2"",
                                        ""transportMode"": ""rail""
                                    }
                                }
                            ]
                        },
                        {
                            ""duration"": 5400,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T10:30:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T12:00:00+02:00"",
                                    ""mode"": ""bus"",
                                    ""distance"": 55000,
                                    ""line"": {
                                        ""publicCode"": ""100"",
                                        ""transportMode"": ""bus""
                                    }
                                }
                            ]
                        }
                    ]
                }
            }
        }";

        public const string ResponseWithWaterTransport = @"{
            ""data"": {
                ""trip"": {
                    ""tripPatterns"": [
                        {
                            ""duration"": 1800,
                            ""legs"": [
                                {
                                    ""expectedStartTime"": ""2026-05-01T08:00:00+02:00"",
                                    ""expectedEndTime"": ""2026-05-01T08:30:00+02:00"",
                                    ""mode"": ""water"",
                                    ""distance"": 5000,
                                    ""line"": {
                                        ""id"": ""RUT:Line:Ferry1"",
                                        ""publicCode"": ""B1"",
                                        ""name"": ""Ferry B1"",
                                        ""transportMode"": ""water"",
                                        ""transportSubmode"": ""localPassengerFerry""
                                    },
                                    ""fromPlace"": {
                                        ""name"": ""Aker Brygge""
                                    },
                                    ""toPlace"": {
                                        ""name"": ""Nesodden""
                                    }
                                }
                            ]
                        }
                    ]
                }
            }
        }";
    }
}
