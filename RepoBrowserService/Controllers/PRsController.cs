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

namespace RepoBrowserService.Controllers
{
    [Route("api/prs")]
    [ApiController]
    public class PRsController : Controller
    {
        // Configuration from appsettings
        private ILogger _logger;
        private IStorageRepository _orgRepository;

        public PRsController(ILogger<PRsController> logger, IOptions<RepositorySettings> repoSettings)
        {
            if (logger == null)
            {
                throw new NullReferenceException("Logger must not be null.");
            }
            _logger = logger;
            ParseConfiguration(repoSettings);
        }

        // GET api/prs?orgName={something?}
        [HttpGet]
        public ActionResult Get([FromQuery]string orgName)
        {
            if (string.IsNullOrEmpty(orgName))
            {
                _logger.LogError("No orgName query string was provided.");
                return new BadRequestResult();
            }
            return Json(orgName);
        }

        // GET api/prs/{id}
        [HttpGet("{id}")]
        public ActionResult Get(int id)
        {
            // Check if organization exists

            // Then fetch data from 
            _logger.LogError("Fetching PRs for organization ID: " + id);
            return Json(id);
        }

        private void ParseConfiguration(IOptions<RepositorySettings> repoSettings)
        {
            _orgRepository = new InMemoryRepository("Organization");
            // Add all organizations to the in-memory repository
        }

        private bool DoesOrganizationExist(int id)
        {
            if (_orgRepository.Read(id) == null) { return false; }
            return true;
        }
    }
}
