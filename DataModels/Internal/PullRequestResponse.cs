using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DataModels.Internal
{
    public class PullRequestResponse : RepoResponse
    {
        public int TotalCount { get { return PullRequests.Count; } }

        // For now and for simplicity, just use Github as our "internal" data model.
        public List<PullRequest> PullRequests = new List<PullRequest>();

    }
}
