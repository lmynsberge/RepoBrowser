﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using gh = DataModels.Github;
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
                var prResults = JsonConvert.DeserializeObject<List<gh.PullRequest>>((httpResponse.Content.ReadAsStringAsync()).Result);
                prResponse.PullRequests.AddRange(ConvertGithubPullRequests(prResults));

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
                        requestUri = CreateRequestUri(prRequest.Organization);
                        _makeAnotherRequest = true;
                    }
                    // Otherwise, this will get them 'all' and pagination can decide for more
                    else
                    {
                        requestUri = CreateRequestUri(prRequest.Organization, prRequest.RepoName, prRequest.State.ToString());
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
                        requestUri = CreateRequestUri(prRequest.Organization, _repoList[0], prRequest.State.ToString());
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

        private Uri CreateRequestUri(string org, string repo = "", string state = "")
        {
            if (string.IsNullOrEmpty(repo))
            {
                return new Uri("https://api.github.com/orgs/" + org + "/repos");
            }
            UriBuilder uriBuilder = new UriBuilder(new Uri("https://api.github.com/repos/" + org + "/" + repo + "/pulls"))
            {
                // Add state query always (typically is the default)
                Query = "state=" + state.ToLower() + "&per_page=100"
            };

            return uriBuilder.Uri;
        }

        private List<PullRequest> ConvertGithubPullRequests(List<gh.PullRequest> ghPulls)
        {
            List<PullRequest> pullRequests = new List<PullRequest>();

            foreach(gh.PullRequest ghPull in ghPulls)
            {
                pullRequests.Add(new PullRequest()
                {
                    url = ghPull.url,
                    id = ghPull.id,
                    node_id = ghPull.node_id,
                    html_url = ghPull.html_url,
                    number = ghPull.number,
                    state = ghPull.state,
                    locked = ghPull.locked,
                    title = ghPull.title,
                    user = ghPull.user?.login,
                    body = ghPull.body,
                    created_at = ghPull.created_at,
                    updated_at = ghPull.updated_at,
                    closed_at = ghPull.closed_at,
                    merged_at = ghPull.merged_at,
                    author_association = ghPull.author_association
                });
            }
            return pullRequests;
        }
    }
}

