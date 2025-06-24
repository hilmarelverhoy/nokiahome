using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace NokiaHome.Services
{
    public class EnturGraphQLService : IEnturGraphQLService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientName;
        private const string GraphQLEndpoint = "https://api.entur.io/journey-planner/v3/graphql";

        public EnturGraphQLService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _clientName = configuration["Entur:ClientName"] ?? "nokiahome-app";
            
            _httpClient.BaseAddress = new Uri(GraphQLEndpoint);
        }

        public async Task<string> ExecuteQueryAsync(string query, object? variables = null)
        {
            var request = new
            {
                query = query,
                variables = variables
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            content.Headers.Add("ET-Client-Name", _clientName);

            var response = await _httpClient.PostAsync("", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetTripPatternsAsync(string fromCode, string toCode)
        {
            return await GetTripPatternsAsync(fromCode, null, null, toCode, null, null);
        }

        public async Task<string> GetTripPatternsAsync(
            string? fromCode, string? fromName, double[]? fromCoordinates,
            string? toCode, string? toName, double[]? toCoordinates)
        {
            // Determine which variables we need
            var useFromCode = !string.IsNullOrEmpty(fromCode) && fromCode.Contains(":");
            var useFromCoords = !useFromCode && !string.IsNullOrEmpty(fromName) && fromCoordinates?.Length >= 2;
            var useToCode = !string.IsNullOrEmpty(toCode) && toCode.Contains(":");
            var useToCoords = !useToCode && !string.IsNullOrEmpty(toName) && toCoordinates?.Length >= 2;

            // Build variable declarations
            var variables = new List<string>();
            if (useFromCode) variables.Add("$fromCode: String!");
            if (useFromCoords)
            {
                variables.Add("$fromName: String!");
                variables.Add("$fromLat: Float!");
                variables.Add("$fromLng: Float!");
            }
            if (useToCode) variables.Add("$toCode: String!");
            if (useToCoords)
            {
                variables.Add("$toName: String!");
                variables.Add("$toLat: Float!");
                variables.Add("$toLng: Float!");
            }

            var variableDeclarations = string.Join(", ", variables);

            // Build from location
            string fromLocation;
            if (useFromCode)
            {
                fromLocation = "place: $fromCode";
            }
            else if (useFromCoords)
            {
                fromLocation = "name: $fromName, coordinates: {latitude: $fromLat, longitude: $fromLng}";
            }
            else
            {
                throw new ArgumentException("Invalid from location parameters");
            }

            // Build to location
            string toLocation;
            if (useToCode)
            {
                toLocation = "place: $toCode";
            }
            else if (useToCoords)
            {
                toLocation = "name: $toName, coordinates: {latitude: $toLat, longitude: $toLng}";
            }
            else
            {
                throw new ArgumentException("Invalid to location parameters");
            }

            string query = @"
            query GetTripPatterns(" + variableDeclarations + @") {
              trip(
                from: {" + fromLocation + @"}
                to: {" + toLocation + @"}
                alightSlackList: {slack: 10, modes: air}
              ) {
                tripPatterns {
                  duration
                  streetDistance
                  legs {
                    expectedStartTime
                    expectedEndTime
                    mode
                    distance
                    line {
                      id
                      publicCode
                      name
                      presentation {
                        colour
                        textColour
                      }
                      transportMode
                      transportSubmode
                      description
                      branding {
                        name
                        image
                        id
                        description
                        shortName
                        url
                      }
                      notices {
                        text
                        publicCode
                      }
                      quays {
                        name
                        description
                        stopType
                      }
                    }
                    duration
                    toPlace {
                      name
                      vertexType
                    }
                    fromEstimatedCall {
                      actualArrivalTime
                      actualDepartureTime
                      aimedArrivalTime
                      aimedDepartureTime
                      date
                      quay {
                        description
                        name
                      }
                    }
                    fromPlace {
                      name
                      quay {
                        description
                        name
                      }
                    }
                  }
                  aimedEndTime
                  walkTime
                  waitingTime
                  streetDistance
                  expectedEndTime
                  distance
                  directDuration
                }
              }
            }";

            // Build variables object with only the variables we declared
            var variablesDict = new Dictionary<string, object?>();
            
            if (useFromCode) variablesDict["fromCode"] = fromCode;
            if (useFromCoords)
            {
                variablesDict["fromName"] = fromName;
                variablesDict["fromLat"] = fromCoordinates![1]; // latitude is second
                variablesDict["fromLng"] = fromCoordinates![0]; // longitude is first
            }
            if (useToCode) variablesDict["toCode"] = toCode;
            if (useToCoords)
            {
                variablesDict["toName"] = toName;
                variablesDict["toLat"] = toCoordinates![1];
                variablesDict["toLng"] = toCoordinates![0];
            }

            return await ExecuteQueryAsync(query, variablesDict);
        }
    }
}