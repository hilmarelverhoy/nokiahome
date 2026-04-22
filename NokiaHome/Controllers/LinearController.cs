using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NokiaHome.Models.Linear;
using NokiaHome.Services;
using NokiaHome.Settings;

namespace NokiaHome.Controllers
{
    public class LinearController : Controller
    {
        private readonly ILinearService _linearService;
        private readonly ILogger<LinearController> _logger;
        private readonly LinearSettings _settings;

        public LinearController(
            ILinearService linearService,
            ILogger<LinearController> logger,
            IOptions<LinearSettings> settings)
        {
            _linearService = linearService;
            _logger = logger;
            _settings = settings.Value;
        }

        // GET /Linear/Debug — step-by-step connectivity check
        public async Task<IActionResult> Debug()
        {
            var result = new LinearDebugViewModel
            {
                ConfiguredTeamId = _settings.TeamId
            };

            // Step 1: auth check via viewer query
            try
            {
                result.Viewer = await _linearService.GetViewerAsync();
                result.AuthOk = result.Viewer != null;
            }
            catch (Exception ex)
            {
                result.AuthError = ex.Message;
            }

            // Step 2: fetch teams (only if auth worked)
            if (result.AuthOk)
            {
                try
                {
                    result.Teams = await _linearService.GetTeamsAsync();
                }
                catch (Exception ex)
                {
                    result.TeamsError = ex.Message;
                }
            }

            // Step 3: raw issues query — no team filter, just prove the query works
            if (result.AuthOk)
            {
                try
                {
                    result.RawIssues = await _linearService.GetRawIssuesAsync(first: 5);
                }
                catch (Exception ex)
                {
                    result.RawIssuesError = ex.Message;
                }
            }

            return View(result);
        }

        // GET /Linear
        public async Task<IActionResult> Index(string? stateFilter = null, string? after = null, string? viewId = null)
        {
            try
            {
                var statesTask = _linearService.GetStatesAsync();
                var issuesTask = string.IsNullOrEmpty(viewId)
                    ? _linearService.GetIssuesAsync(stateFilter, first: 25, after)
                    : _linearService.GetIssuesByViewAsync(viewId, first: 25, after);

                await Task.WhenAll(statesTask, issuesTask);

                ViewBag.States = statesTask.Result;

                // Custom views are non-critical — a failure here should not break the issues list
                try
                {
                    ViewBag.CustomViews = await _linearService.GetCustomViewsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load custom views ({Type})", ex.GetType().Name);
                    ViewBag.CustomViews = new List<LinearCustomView>();
                }

                return View(issuesTask.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Linear issues ({Type})", ex.GetType().Name);
                ViewBag.ErrorMessage = $"Could not load issues: [{ex.GetType().Name}] {ex.Message}";
                return View(new LinearIssueListResult());
            }
        }

        // GET /Linear/Detail/{id}
        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction(nameof(Index));

            try
            {
                var statesTask = _linearService.GetStatesAsync();
                var issueTask = _linearService.GetIssueAsync(id);

                await Task.WhenAll(statesTask, issueTask);

                if (issueTask.Result == null)
                {
                    ViewBag.ErrorMessage = "Issue not found.";
                    return View(null);
                }

                ViewBag.States = statesTask.Result;
                return View(issueTask.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load issue {IssueId}", id);
                ViewBag.ErrorMessage = $"Could not load issue: {ex.Message}";
                return View(null);
            }
        }

        // GET /Linear/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var statesTask = _linearService.GetStatesAsync();
                var labelsTask = _linearService.GetLabelsAsync();
                await Task.WhenAll(statesTask, labelsTask);

                var states = statesTask.Result;
                var todoState = states.FirstOrDefault(s =>
                    s.Name.Equals("Todo", StringComparison.OrdinalIgnoreCase))
                    ?? states.FirstOrDefault(s => s.Type == "unstarted");

                return View(new CreateIssueViewModel
                {
                    StateId = todoState?.Id,
                    AvailableLabels = labelsTask.Result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load states/labels for create form");
                ViewBag.ErrorMessage = $"Could not load form data: {ex.Message}";
                return View(new CreateIssueViewModel());
            }
        }

        // POST /Linear/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateIssueViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try { model.AvailableLabels = await _linearService.GetLabelsAsync(); }
                catch { /* keep empty list */ }
                return View(model);
            }

            try
            {
                var issue = await _linearService.CreateIssueAsync(
                    model.Title,
                    model.Description,
                    model.Priority,
                    model.StateId,
                    model.LabelIds.Count > 0 ? model.LabelIds : null);

                return RedirectToAction(nameof(Detail), new { id = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create issue");
                ViewBag.ErrorMessage = $"Could not create issue: {ex.Message}";
                try { model.AvailableLabels = await _linearService.GetLabelsAsync(); }
                catch { /* keep empty list */ }
                return View(model);
            }
        }

        // POST /Linear/UpdateState
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateState(string issueId, string stateId, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(issueId) || string.IsNullOrEmpty(stateId))
                return BadRequest();

            try
            {
                await _linearService.UpdateIssueStateAsync(issueId, stateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update state for issue {IssueId}", issueId);
                TempData["ErrorMessage"] = $"Could not update issue state: {ex.Message}";
            }

            return Redirect(returnUrl ?? Url.Action(nameof(Index))!);
        }

        // POST /Linear/UpdatePriority
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(string issueId, int priority, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(issueId))
                return BadRequest();

            try
            {
                await _linearService.UpdateIssuePriorityAsync(issueId, priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update priority for issue {IssueId}", issueId);
                TempData["ErrorMessage"] = $"Could not update issue priority: {ex.Message}";
            }

            return Redirect(returnUrl ?? Url.Action(nameof(Index))!);
        }

        // GET /Linear/QuickCreate — Step 1: pick priority
        public IActionResult QuickCreate()
        {
            return View();
        }

        // GET /Linear/QuickCreateProject?priority=1 — Step 2: pick project (or none)
        public async Task<IActionResult> QuickCreateProject(int priority)
        {
            try
            {
                var projects = await _linearService.GetProjectsAsync();
                ViewBag.Priority = priority;
                ViewBag.Projects = projects;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load projects for quick create");
                ViewBag.ErrorMessage = $"Could not load projects: {ex.Message}";
                ViewBag.Priority = priority;
                ViewBag.Projects = new List<LinearProject>();
                return View();
            }
        }

        // GET /Linear/QuickCreateForm?priority=1&projectId=X&projectName=Y — Step 3: title + description + labels
        public async Task<IActionResult> QuickCreateForm(int priority, string? projectId, string? projectName)
        {
            try
            {
                var statesTask = _linearService.GetStatesAsync();
                var labelsTask = _linearService.GetLabelsAsync();
                await Task.WhenAll(statesTask, labelsTask);

                var todoState = statesTask.Result.FirstOrDefault(s =>
                    s.Name.Equals("Todo", StringComparison.OrdinalIgnoreCase))
                    ?? statesTask.Result.FirstOrDefault(s => s.Type == "unstarted");

                return View(new QuickCreateFormViewModel
                {
                    Priority = priority,
                    ProjectId = projectId,
                    ProjectName = projectName,
                    StateId = todoState?.Id,
                    AvailableLabels = labelsTask.Result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load states/labels for quick create form");
                return View(new QuickCreateFormViewModel
                {
                    Priority = priority,
                    ProjectId = projectId,
                    ProjectName = projectName
                });
            }
        }

        // POST /Linear/QuickCreateForm — create the issue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCreateForm(QuickCreateFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var issue = await _linearService.CreateIssueAsync(
                    model.Title,
                    model.Description,
                    model.Priority,
                    model.StateId,
                    labelIds: model.LabelIds.Count > 0 ? model.LabelIds : null,
                    projectId: string.IsNullOrEmpty(model.ProjectId) ? null : model.ProjectId);

                return RedirectToAction(nameof(Detail), new { id = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to quick-create issue");
                ViewBag.ErrorMessage = $"Could not create issue: {ex.Message}";
                try { model.AvailableLabels = await _linearService.GetLabelsAsync(); } catch { /* keep empty */ }
                return View(model);
            }
        }

        // GET /Linear/KjopCreate — minimal title form, kjøp label + medium priority pre-applied
        public async Task<IActionResult> KjopCreate()
        {
            try
            {
                var statesTask = _linearService.GetStatesAsync();
                var labelsTask = _linearService.GetLabelsAsync();
                await Task.WhenAll(statesTask, labelsTask);

                var todoState = statesTask.Result.FirstOrDefault(s =>
                    s.Name.Equals("Todo", StringComparison.OrdinalIgnoreCase))
                    ?? statesTask.Result.FirstOrDefault(s => s.Type == "unstarted");

                var kjopLabel = labelsTask.Result.FirstOrDefault(l =>
                    l.Name.Equals("kjøp", StringComparison.OrdinalIgnoreCase));

                ViewBag.KjopLabelId = kjopLabel?.Id;
                ViewBag.KjopLabelFound = kjopLabel != null;
                ViewBag.StateId = todoState?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load data for kjøp create");
                ViewBag.ErrorMessage = $"Could not load form data: {ex.Message}";
            }

            return View();
        }

        // POST /Linear/KjopCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KjopCreate(string title, string? stateId, string? kjopLabelId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                ViewBag.ErrorMessage = "Title is required.";
                return View();
            }

            try
            {
                var labelIds = string.IsNullOrEmpty(kjopLabelId)
                    ? null
                    : new List<string> { kjopLabelId };

                var issue = await _linearService.CreateIssueAsync(
                    title,
                    description: null,
                    priority: 3, // Medium
                    stateId,
                    labelIds: labelIds);

                return RedirectToAction(nameof(Detail), new { id = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create kjøp issue");
                ViewBag.ErrorMessage = $"Could not create issue: {ex.Message}";
                return View();
            }
        }
    }
}
