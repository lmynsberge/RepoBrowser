using System;
using System.Collections.Generic;
using DataModels.Internal;
using RepoBrowser;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;

namespace RepoBrowserService.Configuration
{
    public class RepositorySettings
    {
        public List<Organization> OrganizationRepoSearch { get; set; } = new List<Organization>();
        public List<RepoBrowserConfiguration> Repositories { get; set; } = new List<RepoBrowserConfiguration>();
    }

}
