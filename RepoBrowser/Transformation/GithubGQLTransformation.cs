using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using DataModels.Internal;
using Newtonsoft.Json;
using gh = DataModels.Github;

namespace RepoBrowser.Transformation
{
    public class GithubGQLTransformation : ITransformationService
    {
        // Base URI for all GraphQL requests
        private readonly static Uri s_ghGQLEndpoint = new Uri("https://api.github.com/graphql");

        // Tracks the repos that need more requests
        private Dictionary<string, int> _repoList;
        private Dictionary<string, string> _startingCursorList = new Dictionary<string, string>();
        private string _currentKey = string.Empty;
        // Used to track the cursor or "next" piece for pagination
        private string _nextCursor = string.Empty;

        public bool AfterResponse(HttpResponseMessage httpResponse, RepoResponse repoResponse)
        {
            if (repoResponse is PullRequestResponse)
            {
                PullRequestResponse prResponse = (PullRequestResponse)repoResponse;
                // If a failure with the response, always exit cleanly.
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return false;
                }

                // Add the result (right now the GH REST response "is" our internal data model, otherwise other conversion would be necessary)
                var prResults = JsonConvert.DeserializeObject<gh.GraphQLPullRequest>((httpResponse.Content.ReadAsStringAsync()).Result);
                prResponse.PullRequests.AddRange(ConvertGithubPullRequests(prResults));

                // No pagination, then we're done
                if (string.IsNullOrEmpty(_nextCursor))
                {
                    return false;
                }
                // Otherwise keep going
                return true;
            }

            return false;
        }

        public bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest)
        {
            // For GraphQL, we're always a post and the same endpoint.
            httpRequest.RequestUri = s_ghGQLEndpoint;
            httpRequest.Method = HttpMethod.Post;

            if (repoRequest is PullRequestRequest)
            {
                PullRequestRequest prRequest = (PullRequestRequest)repoRequest;

                // Add in the request query string post
                httpRequest.Content = new StringContent(GetJSONQueryString(prRequest.Organization, _nextCursor), Encoding.UTF8, "application/json");

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Fetches the GraphQL query
        /// </summary>
        /// <remarks>
        /// Gets the GraphQL query string. For simplicity and to understand the form, this has been done explicitly, but realistically, it's easier
        /// to use the GraphQL-dotnet library. I've actually ported that library over to support dotnet-core without mono in my personal repo.
        /// </remarks>
        /// <returns>The JSONQ uery string.</returns>
        /// <param name="organization">Organization.</param>
        /// <param name="continueCursor">Continue cursor.</param>
        private string GetJSONQueryString(string organization, string continueCursor = "")
        {
            if (!string.IsNullOrEmpty(continueCursor))
            {
                continueCursor = ", after: \\\"" + continueCursor + "\\\" ";
            }

            string result = "{ \"query\": \"query { organization(login: " + organization + ") " +
                "{ repositories(first: 100) { totalCount, nodes { name, pullRequests(first:100" + continueCursor + ") " +
                "{ pageInfo { endCursor }, totalCount, edges { node { " +
                "url, databaseId, id, permalink, number, state, locked, title, author { login }, body, createdAt, updatedAt, closedAt, mergedAt,  authorAssociation  }  } } } } } }\" }";

            return result;
        }

        private List<PullRequest> ConvertGithubPullRequests(gh.GraphQLPullRequest ghPulls)
        {
            List<PullRequest> prList = new List<PullRequest>();
            // First loop through repos to setup pagination
            //  Track each repo name and then track the current count of entries within the dictionary
            //  Compare this to the total count

            // This is the first instance if dictionary is null
            if (_repoList == null)
            {
                _repoList = new Dictionary<string, int>();
                bool firstPiece = true;
                foreach(gh.RepoNode repoNode in ghPulls.data.organization.repositories.nodes)
                {
                    // Now check if what was returned wasn't equal to the total (meaning we didn't get it all in one grab and add it)
                    if (repoNode.pullRequests.edges.Count != repoNode.pullRequests.totalCount)
                    {
                        _repoList.Add(repoNode.name, repoNode.pullRequests.edges.Count);
                        _startingCursorList.Add(repoNode.name, repoNode.pullRequests.pageInfo.endCursor);
                        // If the first one, let's use this to paginate
                        if (firstPiece)
                        {
                            _currentKey = repoNode.name;
                            _nextCursor = repoNode.pullRequests.pageInfo.endCursor;
                            firstPiece = false;
                        }
                    }

                    // Still add them
                    AddGraphQLToList(prList, repoNode.pullRequests);
                }
                return prList;
            }

            // Quick check in-case
            if (_repoList.Count < 1) { return prList; }

            // This means we're paginating, so check where we are at - look over all the repo nodes and find the one that matches our first repo dictionary
            foreach (gh.RepoNode repoNode in ghPulls.data.organization.repositories.nodes)
            {
                if (repoNode.name != _currentKey)
                {
                    continue;
                }

                // Same key, so check if we're done paging and page the next one
                int newTotal = repoNode.pullRequests.edges.Count + _repoList[_currentKey];
                if (newTotal >= repoNode.pullRequests.totalCount)
                {
                    _repoList.Remove(_currentKey);
                    // Nothing left to paginate
                    if (_repoList.Count < 1)
                    {
                        _currentKey = string.Empty;
                        _nextCursor = string.Empty;
                    }
                    // Look through remaining keys, grab the first and add in the apropriate cursor for pagination
                    else
                    {
                        foreach (string repoName in _repoList.Keys)
                        {
                            _currentKey = repoName;
                            _nextCursor = _startingCursorList[repoName];
                            break;
                        }
                    }
                }
                // Not done paginating, just grab the next cursor and add in the update
                else
                {
                    _repoList[_currentKey] = newTotal;
                    _nextCursor = repoNode.pullRequests.pageInfo.endCursor;
                }

                // Still add them (but only the one we're paginating - not the others since those are repeats
                AddGraphQLToList(prList, repoNode.pullRequests);

                // No continuing once we get the one we're paginating on
                break;
            }

            return prList;

        }

        private void AddGraphQLToList(List<PullRequest> prList, gh.PullRequests pullRequests)
        {
            int counter = 0;
            foreach (gh.PullRequestEdge prEdge in pullRequests.edges)
            {
                counter++;
                gh.PullRequestNode prNode = prEdge.node;
                try
                {
                    prList.Add(new PullRequest()
                    {
                        url = prNode.url,
                        id = prNode.databaseId,
                        node_id = prNode.id,
                        html_url = prNode.permalink,
                        number = prNode.number,
                        state = prNode.state,
                        locked = prNode.locked,
                        title = prNode.title,
                        user = prNode?.author?.login,
                        body = prNode.body,
                        created_at = prNode.createdAt,
                        updated_at = prNode.updatedAt,
                        closed_at = prNode.closedAt,
                        merged_at = prNode.mergedAt,
                        author_association = prNode.authorAssociation
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception("Unexpected lack of item when parsing: " + counter + " - " + ex.Message + " - " + JsonConvert.SerializeObject(prNode));
                }
            }

        }
    }
}
