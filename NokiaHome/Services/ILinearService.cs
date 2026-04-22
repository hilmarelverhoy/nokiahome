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

        /// <summary>Updates an existing issue's state.</summary>
        Task UpdateIssueStateAsync(string issueId, string stateId);

        /// <summary>Updates an existing issue's priority (0=None, 1=Urgent, 2=High, 3=Medium, 4=Low).</summary>
        Task UpdateIssuePriorityAsync(string issueId, int priority);
    }
}
