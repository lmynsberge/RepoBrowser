using System.Collections.Generic;
using DataModels.Internal;
using RepoBrowser;

namespace RepoBrowserService.Configuration
{
    /// <summary>
    /// Repository settings class to pull from the appsettings JSON file.
    /// </summary>
    public class RepositorySettings
    {
        public List<Organization> OrganizationRepoSearch { get; set; } = new List<Organization>();
        public List<RepoBrowserConfiguration> Repositories { get; set; } = new List<RepoBrowserConfiguration>();
    }

}
