using NokiaHome.Models.Linear;

namespace NokiaHome.Services
{
    public interface ILinearService
    {
        /// <summary>Returns the authenticated user — the simplest auth check.</summary>
        Task<LinearViewerResult?> GetViewerAsync();

        /// <summary>Returns all teams accessible to the authenticated user.</summary>
        Task<List<LinearTeam>> GetTeamsAsync();

        /// <summary>Returns the first N issues with no filter — used to verify the query works and find team IDs.</summary>
        Task<List<LinearIssue>> GetRawIssuesAsync(int first = 5);

        /// <summary>
        /// Returns a paginated list of issues for the configured team,
        /// optionally filtered by workflow state ID.
        /// </summary>
        Task<LinearIssueListResult> GetIssuesAsync(
            string? stateFilter = null,
            int first = 25,
            string? after = null);

        /// <summary>Returns a single issue including its comments.</summary>
        Task<LinearIssue?> GetIssueAsync(string id);

        /// <summary>Returns all custom views accessible to the authenticated user.</summary>
        Task<List<LinearCustomView>> GetCustomViewsAsync();

        /// <summary>Returns issues shown by a specific custom view, using the view's stored filter.</summary>
        Task<LinearIssueListResult> GetIssuesByViewAsync(string viewId, int first = 25, string? after = null);

        /// <summary>Returns all issue labels for the configured team.</summary>
        Task<List<LinearIssueLabel>> GetLabelsAsync();

        /// <summary>Returns all workflow states for the configured team.</summary>
        Task<List<LinearWorkflowState>> GetStatesAsync();

        /// <summary>Creates a new issue in the configured team.</summary>
        Task<LinearIssue> CreateIssueAsync(
            string title,
            string? description,
            int priority,
            string? stateId,
            List<string>? labelIds = null,
            string? projectId = null);

        /// <summary>Returns all projects accessible to the authenticated user.</summary>
        Task<List<LinearProject>> GetProjectsAsync();

        /// <summary>Returns a single project with its paginated list of issues.</summary>
        Task<LinearProjectDetailViewModel> GetProjectDetailAsync(string projectId, int first = 25, string? after = null);

        /// <summary>Updates an existing issue's state.</summary>
        Task UpdateIssueStateAsync(string issueId, string stateId);

        /// <summary>Updates an existing issue's priority (0=None, 1=Urgent, 2=High, 3=Medium, 4=Low).</summary>
        Task UpdateIssuePriorityAsync(string issueId, int priority);

        /// <summary>Adds a comment to an existing issue.</summary>
        Task AddCommentAsync(string issueId, string body);

        /// <summary>Updates an issue's title.</summary>
        Task UpdateIssueTitleAsync(string issueId, string title);

        /// <summary>Updates an issue's description (markdown).</summary>
        Task UpdateIssueDescriptionAsync(string issueId, string? description);

        /// <summary>Updates an issue's label set (replaces all labels).</summary>
        Task UpdateIssueLabelsAsync(string issueId, List<string> labelIds);

        /// <summary>Moves an issue to a different project (or removes it from projects when projectId is null).</summary>
        Task UpdateIssueProjectAsync(string issueId, string? projectId);

        /// <summary>Archives an issue (soft-delete).</summary>
        Task ArchiveIssueAsync(string issueId);

        /// <summary>Creates a new project.</summary>
        Task<LinearProject> CreateProjectAsync(string name, string? description, string? color, string? icon);

        /// <summary>Updates a project's state (planned, inProgress, paused, completed, cancelled).</summary>
        Task UpdateProjectStateAsync(string projectId, string state);
    }
}
