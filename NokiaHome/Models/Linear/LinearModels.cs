using System.Text.Json.Serialization;

namespace NokiaHome.Models.Linear
{
    // ---------------------------------------------------------------------------
    // Top-level GraphQL response wrappers
    // ---------------------------------------------------------------------------

    public class LinearIssueListResponse
    {
        [JsonPropertyName("data")]
        public IssueListData? Data { get; set; }
    }

    public class IssueListData
    {
        [JsonPropertyName("issues")]
        public LinearIssueConnection? Issues { get; set; }
    }

    public class LinearIssueDetailResponse
    {
        [JsonPropertyName("data")]
        public IssueDetailData? Data { get; set; }
    }

    public class IssueDetailData
    {
        [JsonPropertyName("issue")]
        public LinearIssue? Issue { get; set; }
    }

    public class LinearTeamsResponse
    {
        [JsonPropertyName("data")]
        public TeamsData? Data { get; set; }
    }

    public class TeamsData
    {
        [JsonPropertyName("teams")]
        public LinearTeamConnection? Teams { get; set; }
    }

    public class LinearViewerResponse
    {
        [JsonPropertyName("data")]
        public ViewerData? Data { get; set; }
    }

    public class ViewerData
    {
        [JsonPropertyName("viewer")]
        public LinearViewerResult? Viewer { get; set; }
    }

    public class LinearViewerResult
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    public class LinearStatesResponse
    {
        [JsonPropertyName("data")]
        public StatesData? Data { get; set; }
    }

    public class StatesData
    {
        [JsonPropertyName("workflowStates")]
        public LinearWorkflowStateConnection? WorkflowStates { get; set; }
    }

    public class LinearMutationResponse
    {
        [JsonPropertyName("data")]
        public MutationData? Data { get; set; }
    }

    public class MutationData
    {
        [JsonPropertyName("issueCreate")]
        public IssueMutationPayload? IssueCreate { get; set; }

        [JsonPropertyName("issueUpdate")]
        public IssueMutationPayload? IssueUpdate { get; set; }

        [JsonPropertyName("commentCreate")]
        public CommentMutationPayload? CommentCreate { get; set; }
    }

    public class LinearArchiveMutationResponse
    {
        [JsonPropertyName("data")]
        public ArchiveMutationData? Data { get; set; }
    }

    public class ArchiveMutationData
    {
        [JsonPropertyName("issueArchive")]
        public SuccessPayload? IssueArchive { get; set; }
    }

    public class SuccessPayload
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    public class LinearProjectMutationResponse
    {
        [JsonPropertyName("data")]
        public ProjectMutationData? Data { get; set; }
    }

    public class ProjectMutationData
    {
        [JsonPropertyName("projectCreate")]
        public ProjectMutationPayload? ProjectCreate { get; set; }
    }

    public class ProjectMutationPayload
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("project")]
        public LinearProject? Project { get; set; }
    }

    public class LinearProjectUpdateMutationResponse
    {
        [JsonPropertyName("data")]
        public ProjectUpdateMutationData? Data { get; set; }
    }

    public class ProjectUpdateMutationData
    {
        [JsonPropertyName("projectUpdate")]
        public SuccessPayload? ProjectUpdate { get; set; }
    }

    public class IssueMutationPayload
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("issue")]
        public LinearIssue? Issue { get; set; }
    }

    public class CommentMutationPayload
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("comment")]
        public LinearComment? Comment { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Connection / pagination wrappers
    // ---------------------------------------------------------------------------

    public class LinearIssueConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearIssue>? Nodes { get; set; }

        [JsonPropertyName("pageInfo")]
        public LinearPageInfo? PageInfo { get; set; }
    }

    public class LinearTeamConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearTeam>? Nodes { get; set; }
    }

    public class LinearWorkflowStateConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearWorkflowState>? Nodes { get; set; }
    }

    public class LinearCommentConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearComment>? Nodes { get; set; }
    }

    public class LinearPageInfo
    {
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("endCursor")]
        public string? EndCursor { get; set; }

        [JsonPropertyName("startCursor")]
        public string? StartCursor { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Core domain types
    // ---------------------------------------------------------------------------

    public class LinearIssue
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Priority: 0 = No priority, 1 = Urgent, 2 = High, 3 = Medium, 4 = Low</summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("priorityLabel")]
        public string PriorityLabel { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("state")]
        public LinearWorkflowState? State { get; set; }

        [JsonPropertyName("assignee")]
        public LinearUser? Assignee { get; set; }

        [JsonPropertyName("team")]
        public LinearTeam? Team { get; set; }

        [JsonPropertyName("comments")]
        public LinearCommentConnection? Comments { get; set; }

        [JsonPropertyName("labels")]
        public LinearIssueLabelConnection? Labels { get; set; }

        [JsonPropertyName("project")]
        public LinearProject? Project { get; set; }

        // Computed display helpers
        public string PriorityBadgeClass => Priority switch
        {
            1 => "danger",
            2 => "warning",
            3 => "primary",
            4 => "secondary",
            _ => "light"
        };

        public string StateBadgeClass => State?.Type switch
        {
            "completed" => "success",
            "cancelled" => "secondary",
            "started" => "primary",
            "unstarted" => "info",
            _ => "light"
        };
    }

    public class LinearWorkflowState
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;

        /// <summary>One of: backlog, unstarted, started, completed, cancelled</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class LinearTeam
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }

    public class LinearUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }
    }

    public class LinearComment
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public LinearUser? User { get; set; }
    }

    public class LinearIssueLabel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;
    }

    public class LinearIssueLabelConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearIssueLabel>? Nodes { get; set; }
    }

    public class LinearLabelsResponse
    {
        [JsonPropertyName("data")]
        public LabelsData? Data { get; set; }
    }

    public class LabelsData
    {
        [JsonPropertyName("issueLabels")]
        public LinearIssueLabelConnection? IssueLabels { get; set; }
    }

    public class LinearCustomView
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        [JsonPropertyName("shared")]
        public bool Shared { get; set; }

        /// <summary>One of: Issue, Project, Initiative, FeedItem</summary>
        [JsonPropertyName("modelName")]
        public string ModelName { get; set; } = string.Empty;

        [JsonPropertyName("owner")]
        public LinearUser? Owner { get; set; }

        [JsonPropertyName("organization")]
        public LinearOrganization? Organization { get; set; }

        public string? Url => Organization?.UrlKey is { } key
            ? $"https://linear.app/{key}/view/{Id}"
            : null;
    }

    public class LinearOrganization
    {
        [JsonPropertyName("urlKey")]
        public string UrlKey { get; set; } = string.Empty;
    }

    public class LinearCustomViewConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearCustomView>? Nodes { get; set; }
    }

    public class LinearCustomViewsResponse
    {
        [JsonPropertyName("data")]
        public CustomViewsData? Data { get; set; }
    }

    public class CustomViewsData
    {
        [JsonPropertyName("customViews")]
        public LinearCustomViewConnection? CustomViews { get; set; }
    }

    public class LinearCustomViewIssuesResponse
    {
        [JsonPropertyName("data")]
        public CustomViewIssuesData? Data { get; set; }
    }

    public class CustomViewIssuesData
    {
        [JsonPropertyName("customView")]
        public LinearCustomViewWithIssues? CustomView { get; set; }
    }

    public class LinearCustomViewWithIssues
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("issues")]
        public LinearIssueConnection? Issues { get; set; }
    }

    public class LinearProject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        /// <summary>One of: planned, inProgress, paused, completed, cancelled</summary>
        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("progress")]
        public double? Progress { get; set; }

        [JsonPropertyName("issueCountByState")]
        public LinearProjectIssueCountByState? IssueCountByState { get; set; }

        public string StateBadgeClass => State switch
        {
            "inProgress" => "primary",
            "completed"  => "success",
            "cancelled"  => "secondary",
            "paused"     => "warning",
            _            => "light"
        };

        public string StateLabel => State switch
        {
            "inProgress" => "In Progress",
            "completed"  => "Completed",
            "cancelled"  => "Cancelled",
            "paused"     => "Paused",
            "planned"    => "Planned",
            _            => State ?? "Unknown"
        };
    }

    public class LinearProjectIssueCountByState
    {
        [JsonPropertyName("started")]
        public int Started { get; set; }

        [JsonPropertyName("unstarted")]
        public int Unstarted { get; set; }

        [JsonPropertyName("completed")]
        public int Completed { get; set; }

        [JsonPropertyName("cancelled")]
        public int Cancelled { get; set; }

        public int Total => Started + Unstarted + Completed + Cancelled;
    }

    public class LinearProjectWithIssues : LinearProject
    {
        [JsonPropertyName("issues")]
        public LinearIssueConnection? Issues { get; set; }
    }

    public class LinearProjectConnection
    {
        [JsonPropertyName("nodes")]
        public List<LinearProject>? Nodes { get; set; }
    }

    public class LinearProjectsResponse
    {
        [JsonPropertyName("data")]
        public ProjectsData? Data { get; set; }
    }

    public class ProjectsData
    {
        [JsonPropertyName("projects")]
        public LinearProjectConnection? Projects { get; set; }
    }

    public class LinearProjectDetailResponse
    {
        [JsonPropertyName("data")]
        public ProjectDetailData? Data { get; set; }
    }

    public class ProjectDetailData
    {
        [JsonPropertyName("project")]
        public LinearProjectWithIssues? Project { get; set; }
    }

    public class LinearProjectDetailViewModel
    {
        public LinearProjectWithIssues Project { get; set; } = new();
        public List<LinearIssue> Issues { get; set; } = new();
        public LinearPageInfo? PageInfo { get; set; }
        public string? AfterCursor { get; set; }
    }

    // ---------------------------------------------------------------------------
    // View models (controller → view)
    // ---------------------------------------------------------------------------

    public class LinearIssueListResult
    {
        public List<LinearIssue> Issues { get; set; } = new();
        public LinearPageInfo? PageInfo { get; set; }
        public string? StateFilter { get; set; }
        public string? AfterCursor { get; set; }
        public string? ActiveViewId { get; set; }
        public string? ActiveViewName { get; set; }
    }

    public class CreateIssueViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Priority { get; set; } = 0;
        public string? StateId { get; set; }
        public List<string> LabelIds { get; set; } = new();
        public List<LinearWorkflowState> AvailableStates { get; set; } = new();
        public List<LinearIssueLabel> AvailableLabels { get; set; } = new();
    }

    public class EditIssueViewModel
    {
        public string IssueId { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> LabelIds { get; set; } = new();
        public string? ProjectId { get; set; }
        public List<LinearIssueLabel> AvailableLabels { get; set; } = new();
        public List<LinearProject> AvailableProjects { get; set; } = new();
    }

    public class CreateProjectViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }

    public class QuickCreateFormViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Priority { get; set; }
        public string? ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? StateId { get; set; }
        public List<string> LabelIds { get; set; } = new();
        public List<LinearIssueLabel> AvailableLabels { get; set; } = new();

        public string PriorityLabel => Priority switch
        {
            1 => "Urgent",
            2 => "High",
            3 => "Medium",
            4 => "Low",
            _ => "No priority"
        };

        public string PriorityBadgeClass => Priority switch
        {
            1 => "danger",
            2 => "warning",
            3 => "info",
            4 => "secondary",
            _ => "light"
        };
    }

    public class LinearDebugViewModel
    {
        public bool AuthOk { get; set; }
        public string? AuthError { get; set; }
        public LinearViewerResult? Viewer { get; set; }
        public List<LinearTeam>? Teams { get; set; }
        public string? TeamsError { get; set; }
        public List<LinearIssue>? RawIssues { get; set; }
        public string? RawIssuesError { get; set; }
        public string ConfiguredTeamId { get; set; } = string.Empty;

        // Request diagnostics
        public string RequestMethod { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public string RequestQueryString { get; set; } = string.Empty;
        public string RequestScheme { get; set; } = string.Empty;
        public string RequestHost { get; set; } = string.Empty;
        public string? RemoteIpAddress { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, string> Cookies { get; set; } = new();
        public Dictionary<string, string> RouteValues { get; set; } = new();
    }
}
