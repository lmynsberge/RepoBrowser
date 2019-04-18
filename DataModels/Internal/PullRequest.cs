using System;

namespace DataModels.Internal
{
    /// <summary>
    /// The internal PullRequest data model
    /// </summary>
    public class PullRequest
    {
        public string url { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string html_url { get; set; }
        public int number { get; set; }
        public string state { get; set; }
        public bool locked { get; set; }
        public string title { get; set; }
        public string user { get; set; }
        public string body { get; set; }
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public DateTime? closed_at { get; set; }
        public DateTime? merged_at { get; set; }
        public string author_association { get; set; }
    }
}
