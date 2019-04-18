namespace DataModels.Internal
{
    public class PullRequestRequest : RepoRequest
    {
        public string Organization { get; set; }
        public PullRequestState State { get; set; }
        public string Head { get; set; }
        public string Base { get; set; }
        public PullRequestSort Sort { get; set; }
        public PullRequestDirection Direction { get; set; }
        public string RepoName { get; set; }

        public enum PullRequestState
        {
            Open,
            Closed,
            All
        }

        public enum PullRequestSort
        {
            Created,
            Updated,
            Popularity,
            LongRunning
        }

        public enum PullRequestDirection
        {
            Ascending,
            Descending
        }

        public override string RepoRequestType { get; set; } = "PullRequestRequest";
    }
}
