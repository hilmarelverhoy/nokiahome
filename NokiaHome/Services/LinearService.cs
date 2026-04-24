using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Linear;
using NokiaHome.Settings;

namespace NokiaHome.Services
{
    public class LinearService : ILinearService
    {
        private readonly HttpClient _httpClient;
        private readonly LinearSettings _settings;
        private readonly ILogger<LinearService> _logger;

        private const string GraphQLEndpoint = "https://api.linear.app/graphql";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public LinearService(
            HttpClient httpClient,
            IOptions<LinearSettings> settings,
            ILogger<LinearService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(GraphQLEndpoint);
            _httpClient.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
        }

        // -----------------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------------

        public async Task<LinearIssueListResult> GetIssuesAsync(
            string? stateFilter = null,
            int first = 25,
            string? after = null)
        {
            const string query = """
                query Issues($first: Int!, $filter: IssueFilter, $after: String) {
                  issues(first: $first, filter: $filter, after: $after, orderBy: updatedAt) {
                    nodes {
                      id
                      identifier
                      title
                      priority
                      priorityLabel
                      url
                      createdAt
                      updatedAt
                      state {
                        id
                        name
                        color
                        type
                      }
                      assignee {
                        id
                        name
                        avatarUrl
                      }
                      team {
                        id
                        name
                        key
                      }
                    }
                    pageInfo {
                      hasNextPage
                      hasPreviousPage
                      endCursor
                      startCursor
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?>
            {
                ["first"] = first,
                ["filter"] = BuildIssueFilter(_settings.TeamId, stateFilter),
            };

            if (after != null)
                variables["after"] = after;

            var response = await ExecuteQueryAsync<LinearIssueListResponse>(query, variables);

            return new LinearIssueListResult
            {
                Issues = response?.Data?.Issues?.Nodes ?? new List<LinearIssue>(),
                PageInfo = response?.Data?.Issues?.PageInfo,
                StateFilter = stateFilter,
                AfterCursor = after
            };
        }

        public async Task<LinearIssue?> GetIssueAsync(string id)
        {
            const string query = """
                query Issue($id: String!) {
                  issue(id: $id) {
                    id
                    identifier
                    title
                    description
                    priority
                    priorityLabel
                    url
                    createdAt
                    updatedAt
                    state {
                      id
                      name
                      color
                      type
                    }
                    assignee {
                      id
                      name
                      avatarUrl
                    }
                    team {
                      id
                      name
                      key
                    }
                    comments {
                      nodes {
                        id
                        body
                        createdAt
                        user {
                          id
                          name
                          avatarUrl
                        }
                      }
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearIssueDetailResponse>(
                query,
                new Dictionary<string, object?> { ["id"] = id });

            return response?.Data?.Issue;
        }

        public async Task<List<LinearWorkflowState>> GetStatesAsync()
        {
            const string query = """
                query WorkflowStates($teamId: ID!) {
                  workflowStates(filter: { team: { id: { eq: $teamId } } }, orderBy: updatedAt) {
                    nodes {
                      id
                      name
                      color
                      type
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearStatesResponse>(
                query,
                new Dictionary<string, object?> { ["teamId"] = _settings.TeamId });

            return response?.Data?.WorkflowStates?.Nodes ?? new List<LinearWorkflowState>();
        }

        public async Task<LinearIssue> CreateIssueAsync(
            string title,
            string? description,
            int priority,
            string? stateId,
            List<string>? labelIds = null,
            string? projectId = null)
        {
            const string mutation = """
                mutation IssueCreate($input: IssueCreateInput!) {
                  issueCreate(input: $input) {
                    success
                    issue {
                      id
                      identifier
                      title
                      priority
                      priorityLabel
                      url
                      createdAt
                      state {
                        id
                        name
                        color
                        type
                      }
                      team {
                        id
                        name
                        key
                      }
                    }
                  }
                }
                """;

            var input = new Dictionary<string, object?>
            {
                ["teamId"] = _settings.TeamId,
                ["title"] = title,
                ["priority"] = priority
            };

            if (!string.IsNullOrEmpty(description))
                input["description"] = description;

            if (!string.IsNullOrEmpty(stateId))
                input["stateId"] = stateId;

            if (labelIds != null && labelIds.Count > 0)
                input["labelIds"] = labelIds;

            if (!string.IsNullOrEmpty(projectId))
                input["projectId"] = projectId;

            var response = await ExecuteQueryAsync<LinearMutationResponse>(
                mutation,
                new Dictionary<string, object?> { ["input"] = input });

            var issue = response?.Data?.IssueCreate?.Issue
                ?? throw new InvalidOperationException("Issue creation failed: no issue returned.");

            return issue;
        }

        public async Task<List<LinearProject>> GetProjectsAsync()
        {
            const string query = """
                query {
                  projects(first: 50) {
                    nodes {
                      id
                      name
                      description
                      color
                      icon
                      state
                      progress
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearProjectsResponse>(query);
            return response?.Data?.Projects?.Nodes ?? new List<LinearProject>();
        }

        public async Task<LinearProjectDetailViewModel> GetProjectDetailAsync(
            string projectId,
            int first = 25,
            string? after = null)
        {
            const string query = """
                query ProjectDetail($id: String!, $first: Int!, $after: String) {
                  project(id: $id) {
                    id
                    name
                    description
                    color
                    icon
                    state
                    progress
                    issues(first: $first, after: $after, orderBy: updatedAt) {
                      nodes {
                        id
                        identifier
                        title
                        priority
                        priorityLabel
                        url
                        createdAt
                        updatedAt
                        state {
                          id
                          name
                          color
                          type
                        }
                        assignee {
                          id
                          name
                          avatarUrl
                        }
                        team {
                          id
                          name
                          key
                        }
                      }
                      pageInfo {
                        hasNextPage
                        hasPreviousPage
                        endCursor
                        startCursor
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["id"] = projectId, ["first"] = first };
            if (!string.IsNullOrEmpty(after))
                variables["after"] = after;

            var response = await ExecuteQueryAsync<LinearProjectDetailResponse>(query, variables);
            var project = response?.Data?.Project ?? new LinearProjectWithIssues();

            return new LinearProjectDetailViewModel
            {
                Project = project,
                Issues = project.Issues?.Nodes ?? new List<LinearIssue>(),
                PageInfo = project.Issues?.PageInfo,
                AfterCursor = after
            };
        }

        public async Task UpdateIssueStateAsync(string issueId, string stateId)
        {
            const string mutation = """
                mutation IssueUpdate($id: String!, $input: IssueUpdateInput!) {
                  issueUpdate(id: $id, input: $input) {
                    success
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearMutationResponse>(
                mutation,
                new Dictionary<string, object?>
                {
                    ["id"] = issueId,
                    ["input"] = new Dictionary<string, object?> { ["stateId"] = stateId }
                });

            var success = response?.Data?.IssueUpdate?.Success ?? false;

            if (!success)
                _logger.LogWarning("UpdateIssueState returned success=false for issue {IssueId}", issueId);
        }

        public async Task UpdateIssuePriorityAsync(string issueId, int priority)
        {
            const string mutation = """
                mutation IssueUpdate($id: String!, $input: IssueUpdateInput!) {
                  issueUpdate(id: $id, input: $input) {
                    success
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearMutationResponse>(
                mutation,
                new Dictionary<string, object?>
                {
                    ["id"] = issueId,
                    ["input"] = new Dictionary<string, object?> { ["priority"] = priority }
                });

            var success = response?.Data?.IssueUpdate?.Success ?? false;

            if (!success)
                _logger.LogWarning("UpdateIssuePriority returned success=false for issue {IssueId}", issueId);
        }

        public async Task AddCommentAsync(string issueId, string body)
        {
            const string mutation = """
                mutation CommentCreate($input: CommentCreateInput!) {
                  commentCreate(input: $input) {
                    success
                    comment {
                      id
                      body
                      createdAt
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearMutationResponse>(
                mutation,
                new Dictionary<string, object?> { ["input"] = new Dictionary<string, object?> { ["issueId"] = issueId, ["body"] = body } });

            var success = response?.Data?.CommentCreate?.Success ?? false;

            if (!success)
                _logger.LogWarning("AddComment returned success=false for issue {IssueId}", issueId);
        }

        // -----------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a structured filter object for the issues query.
        /// Using a typed object lets Linear's GraphQL variables system handle
        /// serialisation correctly instead of building raw GraphQL strings.
        /// </summary>
        private static object BuildIssueFilter(string teamId, string? stateId)
        {
            var filter = new Dictionary<string, object>
            {
                ["team"] = new Dictionary<string, object>
                {
                    ["id"] = new Dictionary<string, object> { ["eq"] = teamId }
                }
            };

            if (!string.IsNullOrEmpty(stateId))
            {
                filter["state"] = new Dictionary<string, object>
                {
                    ["id"] = new Dictionary<string, object> { ["eq"] = stateId }
                };
            }

            return filter;
        }

        public async Task<LinearViewerResult?> GetViewerAsync()
        {
            const string query = """
                query {
                  viewer {
                    id
                    name
                    email
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearViewerResponse>(query);
            return response?.Data?.Viewer;
        }

        public async Task<List<LinearIssue>> GetRawIssuesAsync(int first = 5)
        {
            const string query = """
                query RawIssues($first: Int!) {
                  issues(first: $first, orderBy: updatedAt) {
                    nodes {
                      id
                      identifier
                      title
                      team {
                        id
                        name
                        key
                      }
                      state {
                        name
                        type
                      }
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearIssueListResponse>(
                query,
                new Dictionary<string, object?> { ["first"] = first });

            return response?.Data?.Issues?.Nodes ?? new List<LinearIssue>();
        }

        public async Task<List<LinearCustomView>> GetCustomViewsAsync()
        {
            const string query = """
                query {
                  customViews {
                    nodes {
                      id
                      name
                      description
                      icon
                      color
                      shared
                      modelName
                      owner {
                        id
                        name
                      }
                      organization {
                        urlKey
                      }
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearCustomViewsResponse>(query);
            return response?.Data?.CustomViews?.Nodes ?? new List<LinearCustomView>();
        }

        public async Task<LinearIssueListResult> GetIssuesByViewAsync(
            string viewId,
            int first = 25,
            string? after = null)
        {
            const string query = """
                query ViewIssues($id: String!, $first: Int!, $after: String) {
                  customView(id: $id) {
                    id
                    name
                    issues(first: $first, after: $after, orderBy: updatedAt) {
                      nodes {
                        id
                        identifier
                        title
                        priority
                        priorityLabel
                        url
                        createdAt
                        updatedAt
                        state {
                          id
                          name
                          color
                          type
                        }
                        assignee {
                          id
                          name
                          avatarUrl
                        }
                        team {
                          id
                          name
                          key
                        }
                      }
                      pageInfo {
                        hasNextPage
                        hasPreviousPage
                        endCursor
                        startCursor
                      }
                    }
                  }
                }
                """;

            var variables = new Dictionary<string, object?> { ["id"] = viewId, ["first"] = first };
            if (after != null) variables["after"] = after;

            var response = await ExecuteQueryAsync<LinearCustomViewIssuesResponse>(query, variables);
            var view = response?.Data?.CustomView;

            return new LinearIssueListResult
            {
                Issues = view?.Issues?.Nodes ?? new List<LinearIssue>(),
                PageInfo = view?.Issues?.PageInfo,
                AfterCursor = after,
                ActiveViewId = viewId,
                ActiveViewName = view?.Name
            };
        }

        public async Task<List<LinearIssueLabel>> GetLabelsAsync()
        {
            const string query = """
                query Labels($teamId: ID!) {
                  issueLabels(filter: { team: { id: { eq: $teamId } } }, orderBy: updatedAt) {
                    nodes {
                      id
                      name
                      color
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearLabelsResponse>(
                query,
                new Dictionary<string, object?> { ["teamId"] = _settings.TeamId });

            return response?.Data?.IssueLabels?.Nodes ?? new List<LinearIssueLabel>();
        }

        public async Task<List<LinearTeam>> GetTeamsAsync()
        {
            const string query = """
                query {
                  teams {
                    nodes {
                      id
                      name
                      key
                    }
                  }
                }
                """;

            var response = await ExecuteQueryAsync<LinearTeamsResponse>(query);
            return response?.Data?.Teams?.Nodes ?? new List<LinearTeam>();
        }

        /// <summary>
        /// Sends a GraphQL request and deserialises the response into <typeparamref name="T"/>.
        /// Logs a warning on non-2xx status before re-throwing.
        /// Also checks for GraphQL-level errors in the response body.
        /// </summary>
        private async Task<T?> ExecuteQueryAsync<T>(string query, object? variables = null)
        {
            var requestBody = JsonSerializer.Serialize(new { query, variables });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(string.Empty, content);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Linear API HTTP request failed");
                throw;
            }

            var json = await response.Content.ReadAsStringAsync();

            // Check for GraphQL-level errors (Linear returns HTTP 200 even for auth failures)
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("errors", out var errorsEl) &&
                errorsEl.ValueKind == JsonValueKind.Array &&
                errorsEl.GetArrayLength() > 0)
            {
                var firstMessage = errorsEl[0].TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : "Unknown GraphQL error";
                _logger.LogWarning("Linear GraphQL error: {Error} | Full response: {Json}", firstMessage, json);
                throw new InvalidOperationException($"Linear GraphQL error: {firstMessage}");
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
    }
}
