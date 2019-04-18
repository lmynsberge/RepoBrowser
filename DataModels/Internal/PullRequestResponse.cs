using System.Collections.Generic;

namespace DataModels.Internal
{
    public class PullRequestResponse : RepoResponse
    {
        public int TotalCount { get { return PullRequests.Count; } }

        // For now and for simplicity, just use Github as our "internal" data model.
        public List<PullRequest> PullRequests { get; set; } = new List<PullRequest>();

        public override string RepoResponseType { get; set; } = "PullRequestResponse";
    }
}
