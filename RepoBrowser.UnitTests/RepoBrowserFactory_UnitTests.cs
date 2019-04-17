using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataModels.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;
using Xunit;

namespace RepoBrowser.UnitTests
{
    public class RepoBrowserFactory_UnitTests
    {
        private Organization _gitHubOrg;
        private Organization _bitBucketOrg;
        private Organization _bothOrg;
        private PullRequestRequest _request;
        private MockRepoBrowser _repoBrowser = new MockRepoBrowser();

        RepoBrowserFactory_UnitTests()
        {
            _gitHubOrg = new Organization(1);
            _gitHubOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _gitHubOrg.Repositories.Add(new Organization.Repository()
            {
                Type = RepositoryType.Github,
                Name = "unittestGH"
            });

            _bitBucketOrg = new Organization(2);
            _bitBucketOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _bitBucketOrg.Repositories.Add(new Organization.Repository()
            {
                Type = RepositoryType.Bitbucket,
                Name = "unittestGH"
            });

            _bothOrg = new Organization(3);
            _bothOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _bothOrg.Repositories.Add(_gitHubOrg.Repositories[0]);
            _bothOrg.Repositories.Add(_bitBucketOrg.Repositories[0]);

            _request = new PullRequestRequest();
            _request.State = PullRequestRequest.PullRequestState.All;
        }

        [Fact]
        public void CreateSearchRepository_GH_RepoCreated()
        {
            IRepoBrowser searchRepo = RepoBrowserFactory.CreateSearchRepository(_gitHubOrg);
            Assert.IsType(typeof(GithubRepoBrowser), searchRepo);
        }

        [Fact]
        public void CreateSearchRepository_NoBitBucket_ThrowsException()
        {
            Assert.Throws<NotSupportedException>(() =>
                RepoBrowserFactory.CreateSearchRepository(_bitBucketOrg));
        }

        [Fact]
        public void CreateSearchRepository_BothBitBucketGH_ThrowsException()
        {
            Assert.Throws<NotSupportedException>(() =>
                RepoBrowserFactory.CreateSearchRepository(_bothOrg));
        }

        [Fact]
        public void GetPullRequests_All_Returns10()
        {
            RepoBrowserFactory.GetPullRequests(_request, _repoBrowser);
        }

        [Fact]
        public void GetPullRequests_Open_Returns5()
        {
            _request.State = PullRequestRequest.PullRequestState.Open;
            RepoBrowserFactory.GetPullRequests(_request, _repoBrowser);
        }

        [Fact]
        public void GetPullRequests_NotAuthenticated_Authenticates()
        {
            _repoBrowser.GetAuthenticationService().
            RepoBrowserFactory.GetPullRequests(_request, _repoBrowser);
        }
    }

    public class MockRepoBrowser : IRepoBrowser
    {
        private IAuthenticationService _authService;
        private ITransformationService _transformService;
        private HttpMessageHandler _messageHandler;

        public MockRepoBrowser()
        {
            _authService = new MockAuthenticationService();
            _transformService = new MockTransformationService();
            _messageHandler = new MockHttpMessageHandler();
        }
        public IAuthenticationService GetAuthenticationService()
        {
            return _authService;
        }

        public HttpMessageHandler GetHttpMessageHandler()
        {
            return _messageHandler;
        }

        public ITransformationService GetTransformationService()
        {
            return _transformService;
        }
    }

    public class MockAuthenticationService : IAuthenticationService
    {
        public void AfterResponse(HttpResponse httpResponse)
        {
            return;
        }

        public void Authenticate()
        {
            return;
        }

        public void BeforeRequest(HttpRequest httpRequest)
        {
            return;
        }

        public bool IsAuthenticated()
        {
            return true;
        }
    }

    public class MockTransformationService : ITransformationService
    {
        public RepoResponse AfterResponse(HttpResponse httpResponse)
        {
            throw new NotImplementedException();
        }

        public HttpRequest BeforeRequest(RepoRequest repoRequest)
        {
            throw new NotImplementedException();
        }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
