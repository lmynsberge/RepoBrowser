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

        /// <summary>
        /// Implements the logic after the response for Github's GraphQL API.
        /// </summary>
        /// <returns><c>true</c>, if to run a request again, <c>false</c> otherwise.</returns>
        /// <param name="httpResponse">Http response.</param>
        /// <param name="repoResponse">Repo response.</param>
        public bool AfterResponse(HttpResponseMessage httpResponse, RepoResponse repoResponse)
        {
            if (repoResponse is PullRequestResponse)
            {
                PullRequestResponse prResponse = (PullRequestResponse)repoResponse;
                // If a failure with the response, always exit cleanly.
                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Failed HTTP request to Github's v4/GraphQL API: " + httpResponse.StatusCode + " - " + httpResponse.Content.ReadAsStringAsync().Result);
                }

                // Add the result - if there's a key, then we're using Repo-specific requests
                if (string.IsNullOrEmpty(_currentKey))
                {
                    var prResults = JsonConvert.DeserializeObject<gh.GraphQLPullRequest>((httpResponse.Content.ReadAsStringAsync()).Result);
                    prResponse.PullRequests.AddRange(ParseGraphQLResponse(prResults));
                }
                else
                {
                    var prResults = JsonConvert.DeserializeObject<gh.GraphQLRepoSpecificPullRequest>((httpResponse.Content.ReadAsStringAsync()).Result);
                    prResponse.PullRequests.AddRange(ParseRepoGraphQLResponse(prResults));
                }

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

        /// <summary>
        /// Implements actions before the request for Github's GraphQL API to determine if the request is ready to be made.
        /// </summary>
        /// <returns><c>true</c>, if request was befored, <c>false</c> otherwise.</returns>
        /// <param name="repoRequest">Repo request.</param>
        /// <param name="httpRequest">Http request.</param>
        public bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest)
        {
            // For GraphQL, we're always a post and the same endpoint.
            httpRequest.RequestUri = s_ghGQLEndpoint;
            httpRequest.Method = HttpMethod.Post;

            if (repoRequest is PullRequestRequest)
            {
                PullRequestRequest prRequest = (PullRequestRequest)repoRequest;

                // Add in the request query string post - check for the first time to use the prRequest
                string specificRepo = _currentKey;
                if(string.IsNullOrEmpty(_currentKey))
                {
                    _currentKey = prRequest.RepoName;
                }
                httpRequest.Content = new StringContent(GetJSONQueryString(prRequest.Organization, _nextCursor, _currentKey, prRequest.State), Encoding.UTF8, "application/json");

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
        private string GetJSONQueryString(string organization, string continueCursor = "", string repository = "", PullRequestRequest.PullRequestState? state = null)
        {
            if (!string.IsNullOrEmpty(continueCursor))
            {
                continueCursor = ", after: \\\"" + continueCursor + "\\\" ";
            }

            // Default for GraphQL is all
            string stateString = string.Empty;
            if (state.HasValue && state.Value != PullRequestRequest.PullRequestState.All)
            {
                // GraphQL provides more options than REST (closed is both Merged and Closed)
                if (state.Value == PullRequestRequest.PullRequestState.Closed)
                {
                    stateString = ", states: [CLOSED, MERGED] ";
                }
                else
                {
                    stateString = ", states: [OPEN] ";
                }
            }

            string result = string.Empty;
            // Add escaping and quotes for organization in case it has a hyphen,e tc.
            organization = "\\\"" + organization + "\\\"";
            if (string.IsNullOrEmpty(repository))
            {
                result = "{ \"query\": \"query { organization(login: " + organization + ") " +
                "{ repositories(first: 100) { totalCount, nodes { name, pullRequests(first:100" + stateString + continueCursor + ") " +
                "{ pageInfo { endCursor }, totalCount, edges { node { " +
                "url, databaseId, id, permalink, number, state, locked, title, author { login }, body, createdAt, updatedAt, closedAt, mergedAt,  authorAssociation  }  } } } } } }\" }";
            }
            else
            {
                repository = "\\\"" + repository + "\\\"";
                result = "{ \"query\": \"query { repository(owner: " + organization + ", name: " + repository + " ) " +
                "{ name, pullRequests(first:100" + stateString + continueCursor + ") " +
                "{ pageInfo { endCursor }, totalCount, edges { node { " +
                "url, databaseId, id, permalink, number, state, locked, title, author { login }, body, createdAt, updatedAt, closedAt, mergedAt,  authorAssociation  }  } } } }\" }";
            }

            return result;
        }

        /// <summary>
        /// Gets the type for GraphQL deserialization.
        /// </summary>
        /// <returns>The deserialization type.</returns>
        /// <param name="organization">Organization.</param>
        /// <param name="repository">Repository.</param>
        private Type GetDeserializationType(string organization, string repository)
        {
            // No repo is the large response
            if (string.IsNullOrEmpty(repository))
            {
                return typeof(gh.GraphQLPullRequest);
            }
            else
            {
                return typeof(gh.GraphQLRepoSpecificPullRequest);
            }
        }

        /// <summary>
        /// Parses the GraphQL response for just pull requests
        /// </summary>
        /// <returns>The repo graph QLR esponse.</returns>
        /// <param name="ghPulls">Gh pulls.</param>
        private List<PullRequest> ParseRepoGraphQLResponse(gh.GraphQLRepoSpecificPullRequest resultNode)
        {
            List<PullRequest> prList = new List<PullRequest>();
            // First loop through repos to setup pagination
            //  Track each repo name and then track the current count of entries within the dictionary
            //  Compare this to the total count

            // This is the first instance if dictionary is null - but we know we only have one
            gh.RepoNode repoNode = resultNode.data.repository;
            if (_repoList == null)
            {
                _repoList = new Dictionary<string, int>();
                FirstResponse(repoNode, prList);
                _currentKey = repoNode.name;
                _nextCursor = repoNode.pullRequests.pageInfo.endCursor;
                return prList;
            }

            // Quick check in-case
            if (_repoList.Count < 1) { return prList; }

            SubsequentResponses(repoNode, prList);

            return prList;

        }

        /// <summary>
        /// Converts the github pull requests to internal pull requests. Updates and tracks next links.
        /// </summary>
        /// <returns>The github pull requests.</returns>
        /// <param name="ghPulls">Gh pulls.</param>
        private List<PullRequest> ParseGraphQLResponse(gh.GraphQLPullRequest ghPulls)
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
                    FirstResponse(repoNode, prList);
                    // If the first one, let's use this to paginate
                    if (firstPiece)
                    {
                        _currentKey = repoNode.name;
                        _nextCursor = repoNode.pullRequests.pageInfo.endCursor;
                        firstPiece = false;
                    }
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

                SubsequentResponses(repoNode, prList);

                // No continuing once we get the one we're paginating on
                break;
            }

            return prList;

        }

        /// <summary>
        /// Handles the first pass for a single repo..
        /// </summary>
        /// <returns>The response.</returns>
        /// <param name="repoNode">Repo node.</param>
        /// <param name="prList">Pr list.</param>
        private void FirstResponse(gh.RepoNode repoNode, List<PullRequest> prList)
        {
            // Now check if what was returned wasn't equal to the total (meaning we didn't get it all in one grab and add it)
            if (repoNode.pullRequests.edges.Count != repoNode.pullRequests.totalCount)
            {
                _repoList.Add(repoNode.name, repoNode.pullRequests.edges.Count);
                _startingCursorList.Add(repoNode.name, repoNode.pullRequests.pageInfo.endCursor);
            }

            // Still add them
            ConvertGithubPullRequests(prList, repoNode.pullRequests);
        }

        /// <summary>
        /// Handles paginating and other subsequent requests for a single repo.
        /// </summary>
        /// <returns>The responses.</returns>
        /// <param name="repoNode">Repo node.</param>
        /// <param name="prList">Pr list.</param>
        private void SubsequentResponses(gh.RepoNode repoNode, List<PullRequest> prList)
        {
            // This means we're paginating, so check where we are at - always the same in this function
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
            ConvertGithubPullRequests(prList, repoNode.pullRequests);

        }

        /// <summary>
        /// Does the actual conversion of the GraphQL PR response to the internal PR data model.
        /// </summary>
        /// <param name="prList">Pr list.</param>
        /// <param name="pullRequests">Pull requests.</param>
        private void ConvertGithubPullRequests(List<PullRequest> prList, gh.PullRequests pullRequests)
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
