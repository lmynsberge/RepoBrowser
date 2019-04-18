using System;
using System.Collections.Generic;

namespace DataModels.Github
{
    /// <summary>
    /// Github's GraphQL PullRequest model as defined within our query logic here.
    /// </summary>
    public class GraphQLPullRequest
    {
        public Data data { get; set; }
    }

    public class PageInfo
    {
        public string endCursor { get; set; }
    }

    public class Author
    {
        public string login { get; set; }
    }

    public class PullRequestNode
    {
        public string url { get; set; }
        public int databaseId { get; set; }
        public string id { get; set; }
        public string permalink { get; set; }
        public int number { get; set; }
        public string state { get; set; }
        public bool locked { get; set; }
        public string title { get; set; }
        public Author author { get; set; }
        public string body { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime? updatedAt { get; set; }
        public DateTime? closedAt { get; set; }
        public DateTime? mergedAt { get; set; }
        public string authorAssociation { get; set; }
    }

    public class PullRequestEdge
    {
        public PullRequestNode node { get; set; }
    }

    public class PullRequests
    {
        public PageInfo pageInfo { get; set; }
        public int totalCount { get; set; }
        public List<PullRequestEdge> edges { get; set; }
    }

    public class RepoNode
    {
        public string name { get; set; }
        public PullRequests pullRequests { get; set; }
    }

    public class Repositories
    {
        public int totalCount { get; set; }
        public List<RepoNode> nodes { get; set; }
    }

    public class Organization
    {
        public Repositories repositories { get; set; }
    }

    public class Data
    {
        public Organization organization { get; set; }
    }
}
