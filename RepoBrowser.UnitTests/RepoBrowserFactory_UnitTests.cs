using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataModels.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;
using Xunit;

namespace RepoBrowser.UnitTests
{
    public class RepoBrowserFactory_UnitTests
    {
        private Organization _gitHubOrg;
        private Organization _bitBucketOrg;
        private PullRequestRequest _request;
        private MockRepoBrowser _repoBrowser = new MockRepoBrowser();
        private List<RepoBrowserConfiguration> _repoConfigList = new List<RepoBrowserConfiguration>();

        public RepoBrowserFactory_UnitTests()
        {


            _gitHubOrg = new Organization(1);
            _gitHubOrg.Repository = new Organization.OrgRepository()
            {
                Type = RepositoryType.Github,
                Name = "unittestGH"
            };

            _bitBucketOrg = new Organization(2);
            _bitBucketOrg.Repository = new Organization.OrgRepository()
            {
                Type = RepositoryType.Bitbucket,
                Name = "unittestGH"
            };

            _request = new PullRequestRequest();
            _request.State = PullRequestRequest.PullRequestState.All;

            List<string> scopes = new List<string>() { "mock_scope" };
            _repoConfigList.Add(new RepoBrowserConfiguration()
            {
                TypeName = RepositoryType.Github,
                AuthType = "RepoBrowser.UnitTests.MockAuthenticationService, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                TransformType = "RepoBrowser.UnitTests.MockTransformationService, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                HttpMessageHandlerType = "RepoBrowser.UnitTests.MockHttpMessageHandler, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AuthSettings = new AuthenticationSettings()
                {
                    EnvClientID = "MOCK_CLIENTID",
                    EnvUserName = "MOCK_USERNAME",
                    EnvUserPassword = "MOCK_USERPASS",
                    EnvClientSecret = "MOCK_CLIENTSECRET",
                    Note = "MockNote",
                    OAuth2Endpoint = "http://mock/",
                    Scopes = scopes
                }
            });
            _repoConfigList.Add(new RepoBrowserConfiguration()
            {
                TypeName = RepositoryType.Bitbucket,
                AuthType = "RepoBrowser.UnitTests.MockAuthenticationService, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                TransformType = "RepoBrowser.UnitTests.MockTransformationService, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                HttpMessageHandlerType = "RepoBrowser.UnitTests.MockHttpMessageHandler, RepoBrowser.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                AuthSettings = new AuthenticationSettings()
                {
                    EnvClientID = "MOCK_CLIENTID",
                    EnvUserName = "MOCK_USERNAME",
                    EnvUserPassword = "MOCK_USERPASS",
                    EnvClientSecret = "MOCK_CLIENTSECRET",
                    Note = "MockNote",
                    OAuth2Endpoint = "http://mock/",
                    Scopes = scopes
                }
            });
        }

        [Fact]
        public void CreateSearchRepository_GH_RepoCreated()
        {
            IRepoBrowser searchRepo = RepoBrowserFactory.CreateRepoBrowser(_gitHubOrg, _repoConfigList, null);
            Assert.IsType<GithubRepoBrowser>(searchRepo);
        }

        [Fact]
        public void CreateSearchRepository_NoBitBucket_ThrowsException()
        {
            Assert.Throws<NotSupportedException>(() =>
                RepoBrowserFactory.CreateRepoBrowser(_bitBucketOrg, _repoConfigList, null));
        }

        [Fact]
        public void GetPullRequests_All_Returns10()
        {
            // Setup 10 PRs
            List<PullRequest> pullRequests = new List<PullRequest>();
            for (int i = 0; i < 10; i++)
            {
                pullRequests.Add(new PullRequest());
            }
            _repoBrowser.TransformService.PRResponseToReturn = new PullRequestResponse()
            {
                PullRequests = pullRequests
            };

            PullRequestResponse response = RepoBrowserFactory.GetPullRequests(_request, _repoBrowser).Result;

            Assert.NotNull(response);
            Assert.Equal(10, response.PullRequests.Count);
        }

        [Fact]
        public void GetPullRequests_NotAuthenticated_Authenticates()
        {
            _repoBrowser.AuthService.IsAuthenticatedReturn = false;
            PullRequestResponse response = RepoBrowserFactory.GetPullRequests(_request, _repoBrowser).Result;

            Assert.True(_repoBrowser.AuthService.AuthenticateHit);

            // Restore
            _repoBrowser.AuthService.AuthenticateHit = false;
        }

        [Fact]
        public void GetPullRequests_Authentication_AllHit()
        {
            _repoBrowser.AuthService.AfterResponseHit = false;
            _repoBrowser.AuthService.BeforeRequestHit = false;
            _repoBrowser.AuthService.AuthenticateHit = false;
            _repoBrowser.AuthService.IsAuthenticatedReturn = false;
            PullRequestResponse response = RepoBrowserFactory.GetPullRequests(_request, _repoBrowser).Result;

            Assert.True(_repoBrowser.AuthService.AuthenticateHit);
            Assert.True(_repoBrowser.AuthService.AfterResponseHit);
            Assert.True(_repoBrowser.AuthService.BeforeRequestHit);
        }
    }

    public class MockRepoBrowser : IRepoBrowser
    {
        public MockAuthenticationService AuthService;
        public MockTransformationService TransformService;
        public MockHttpMessageHandler MessageHandler;

        public MockRepoBrowser()
        {
            AuthService = new MockAuthenticationService(null);
            TransformService = new MockTransformationService();
            MessageHandler = new MockHttpMessageHandler();
        }
        public IAuthenticationService GetAuthenticationService()
        {
            return AuthService;
        }

        public HttpMessageHandler GetHttpMessageHandler()
        {
            return MessageHandler;
        }

        public ITransformationService GetTransformationService()
        {
            return TransformService;
        }
    }

    public class MockAuthenticationService : IAuthenticationService
    {
        public bool AfterResponseHit = false;
        public bool AuthenticateHit = false;
        public bool BeforeRequestHit = false;
        public bool IsAuthenticatedReturn = false;

        public MockAuthenticationService(AuthenticationSettings authSettings) { }

        public void AfterResponse(HttpResponseMessage httpResponse)
        {
            AfterResponseHit = true;
            return;
        }

        public Task Authenticate(HttpMessageHandler handler)
        {
            AuthenticateHit = true;
            return Task.CompletedTask;
        }

        public void BeforeRequest(HttpRequestMessage httpRequest)
        {
            BeforeRequestHit = true;
        }

        public bool IsAuthenticated()
        {
            return IsAuthenticatedReturn;
        }
    }

    public class MockTransformationService : ITransformationService
    {
        public bool AbortRequest = false;
        public bool RepeatResponse = false;
        public PullRequestResponse PRResponseToReturn;

        public bool AfterResponse(HttpResponseMessage httpResponse, RepoResponse repoResponse)
        {
            if (repoResponse.RepoResponseType == "PullRequestResponse")
            {
                PullRequestResponse prResponse = (PullRequestResponse)repoResponse;
                if (PRResponseToReturn != null)
                {
                    prResponse.PullRequests = PRResponseToReturn.PullRequests;
                }
            }

            return RepeatResponse;
        }

        public bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest)
        {
            httpRequest.RequestUri = new Uri("http://localhost:5000");
            return !AbortRequest;
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        public bool ReturnSuccess = true;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();

            httpResponseMessage.StatusCode = ReturnSuccess ? System.Net.HttpStatusCode.Accepted : System.Net.HttpStatusCode.NotFound;

            return Task<HttpResponseMessage>.Factory.StartNew(() => httpResponseMessage);
        }
    }
}
