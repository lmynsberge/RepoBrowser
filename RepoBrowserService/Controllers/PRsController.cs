using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RepoBrowserService.Configuration;
using RepoBrowser;
using RepoBrowser.Storage;
using DataModels.Internal;
using Microsoft.Extensions.Caching.Memory;

namespace RepoBrowserService.Controllers
{
    [Route("api/prs")]
    [ApiController]
    public class PRsController : Controller
    {
        // Configuration from appsettings
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private IStorageRepository _orgRepository;
        private List<RepoBrowserConfiguration> _repoConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:RepoBrowserService.Controllers.PRsController"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="repoSettings">Repo settings.</param>
        public PRsController(ILogger<PRsController> logger, IOptions<RepositorySettings> repoSettings, IMemoryCache memoryCache)
        {
            _logger = logger ?? throw new NullReferenceException("Logger must not be null.");
            _memoryCache = memoryCache;
            ParseConfiguration(repoSettings);
        }

        /// <summary>
        /// Handles GET api/prs/{id} along with query parameters 'repo' and 'state'
        /// </summary>
        /// <returns>The JSON response of the request</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="repo">Repo.</param>
        /// <param name="state">State.</param>
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(int id, 
            [FromQuery]string repo, [FromQuery]string state)
        {
            // TODO: Remove - for quick metric purposes only
            DateTime start = DateTime.UtcNow;

            // Check if organization exists, otherwise not found
            if (!DoesOrganizationExist(id, out Organization organization))
            {
                return NotFound();
            }

            // Then fetch data from PRs using that organization
            _logger.LogDebug("Fetching PRs for organization ID: " + id);

            // Create internal PullRequestRequest from data and fetch it
            PullRequestRequest request = CreatePullRequest(repo, state, organization.Repository.Name);
            IRepoBrowser browser = RepoBrowserFactory.CreateRepoBrowser(organization, _repoConfig, _memoryCache);
            PullRequestResponse response = await RepoBrowserFactory.GetPullRequests(request, browser);

            // TODO: Remove - for quick metric purposes only
            _logger.LogError("EndTime: " + (new TimeSpan(DateTime.UtcNow.Ticks - start.Ticks)).TotalSeconds);
            return Json(response);
        }

        /// <summary>
        /// Parses the configuration for the app
        /// </summary>
        /// <param name="repoSettings">Repo settings.</param>
        private void ParseConfiguration(IOptions<RepositorySettings> repoSettings)
        {
            if (repoSettings == null) { return; }

            // Add all organizations to the in-memory repository
            //  For this repository, let the configurer decide the ID
            _orgRepository = new InMemoryRepository("Organization");
            repoSettings.Value?.OrganizationRepoSearch?.ForEach(
                org => _orgRepository.Update(org.ID, org)
                );

            // Assing the repo configuration
            _repoConfig = repoSettings.Value?.Repositories;
        }

        /// <summary>
        /// Checks whether or not the organization being requested already exists
        /// </summary>
        /// <returns><c>true</c>, if organization exists <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
        private bool DoesOrganizationExist(int id, out Organization organization)
        {
            organization = (Organization)_orgRepository.Read(id);
            if ( organization == null) { return false; }
            return true;
        }

        /// <summary>
        /// Creates the internal PR request object
        /// </summary>
        /// <returns>The pull request.</returns>
        /// <param name="repo">Repo.</param>
        /// <param name="state">State.</param>
        private PullRequestRequest CreatePullRequest(string repo, string state, string orgName )
        {
            PullRequestRequest pullRequest = new PullRequestRequest();
            pullRequest.Organization = orgName;
            pullRequest.RepoName = repo;

            // Try to parse, otherwise take default
            if (Enum.TryParse<PullRequestRequest.PullRequestState>(state, true, out PullRequestRequest.PullRequestState  enumState))
            {
                pullRequest.State = enumState;
            }

            return pullRequest;
        }
    }
}
