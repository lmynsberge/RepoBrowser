using System;
using DataModels.Internal;
using Microsoft.AspNetCore.Mvc;

namespace RepoBrowser
{
    public static class RepoBrowserFactory
    {
        public static ISearchRepository GetRepositorySearch(Organization organization)
        {
            throw new NotImplementedException();
        }

        public static JsonResult GetPullRequests(ISearchRepository searchRepository)
        {
            throw new NotImplementedException();
        }
    }
}
