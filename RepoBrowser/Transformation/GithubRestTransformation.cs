using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using DataModels.Github;
using DataModels.Internal;
using Newtonsoft.Json;

namespace RepoBrowser.Transformation
{
    public class GithubRestTransformation : ITransformationService
    {
        private bool _makeAnotherRequest = false;
        private List<string> _repoList = new List<string>();
        private Uri _nextPage;

        public GithubRestTransformation()
        {
        }

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

                // If the URI contains 'orgs' we must have been getting the list of repos
                if (httpResponse.RequestMessage.RequestUri.AbsoluteUri.Contains("/orgs/"))
                {
                    var repoResults = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>((httpResponse.Content.ReadAsStringAsync()).Result);
                    foreach (Dictionary<string, object> repo in repoResults)
                    {
                        if (repo.ContainsKey("name"))
                        {
                            _repoList.Add((string)repo["name"]);
                        }
                    }

                    return _makeAnotherRequest;
                }

                // Add the result (right now the GH REST response "is" our internal data model, otherwise other conversion would be necessary)
                var prResults = JsonConvert.DeserializeObject<List<PullRequest>>((httpResponse.Content.ReadAsStringAsync()).Result);
                prResponse.PullRequests.AddRange(prResults);

                var nextLink = ReturnNextLinkPage(httpResponse.Headers);
                if (string.IsNullOrEmpty(nextLink))
                {
                    // Remove the first one (this is the one we've been working on
                    _repoList.RemoveAt(0);
                    // Reset pagination
                    _nextPage = null;
                    if (_repoList.Count < 1) { return false; }
                }
                else
                {
                    _nextPage = new Uri(nextLink);
                }
                return _makeAnotherRequest;
            }

            return false;
        }

        public bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest)
        {
            if (repoRequest is PullRequestRequest)
            {
                PullRequestRequest prRequest = (PullRequestRequest)repoRequest;

                // First time this is false because no notion of making another request yet.
                Uri requestUri = null;
                if (!_makeAnotherRequest)
                {
                    // If no repo name, first get the list of repos and we'll need to go again
                    if (string.IsNullOrEmpty(prRequest.RepoName))
                    {
                        requestUri = new Uri("https://api.github.com/orgs/" + prRequest.Organization + "/repos");
                        _makeAnotherRequest = true;
                    }
                    // Otherwise, this will get them 'all' and pagination can decide for more
                    else
                    {
                        requestUri = new Uri("https://api.github.com/repos/" + prRequest.Organization + "/" + prRequest.RepoName + "/pulls");
                    }
                }
                // Otherwise make we need to make the repo request and check pagination
                else
                {
                    // No next page, so don't request pagination
                    if (_nextPage == null)
                    {
                        // Nothing to do and no more repos
                        if (_repoList.Count < 1) { return false; }
                        requestUri = new Uri("https://api.github.com/repos/" + prRequest.Organization + "/" + _repoList[0] + "/pulls");
                    }
                    else
                    {
                        requestUri = _nextPage;
                    }
                }

                httpRequest.RequestUri = requestUri;

                return true;
            }
            else
            {
                return false;
            }
        }

        private string ReturnNextLinkPage(HttpResponseHeaders headers)
        {
            // Now check whether we should paginate and remove this repo if not.
            headers.TryGetValues("Link", out IEnumerable<string> links);
            string nextLink = string.Empty;
            if (links == null) { return nextLink; }
            foreach (string link in links)
            {
                List<string> availableLinks = new List<string>(link.Split(','));
                foreach (string availableLink in availableLinks)
                {
                    string[] currentLinkPieces = availableLink.Split(';');
                    if (currentLinkPieces.Length < 2) { continue; }
                    string linkType = currentLinkPieces[1];
                    if (linkType.Contains("\"next\""))
                    {
                        nextLink = ExtractLink(availableLink);
                    }
                }
                break;
            }

            return nextLink;
        }

        private string ExtractLink(string githubLinkPiece)
        {
            return githubLinkPiece.Split('>')[0].Trim(' ').Trim('<');
        }
    }
}
