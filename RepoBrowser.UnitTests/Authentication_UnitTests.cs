using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoBrowser.Authentication;
using Xunit;

namespace RepoBrowser.UnitTests
{
    public class Authentication_UnitTests
    {
        private BasicAuthentication _basicAuth;
        private OAuth2Authentication _oauth2Auth;
        private AuthenticationSettings _authSettings;

        public Authentication_UnitTests()
        {
            _authSettings = new AuthenticationSettings()
            {
                EnvClientID = "MOCK_CLIENTID",
                EnvUserName = "MOCK_USERNAME",
                EnvUserPassword = "MOCK_USERPASS",
                EnvClientSecret = "MOCK_CLIENTSECRET",
                Note = "MockNote",
                OAuth2Endpoint = "http://mock/",
                Scopes = new List<string>() { "mock_scope" }
            };

            Environment.SetEnvironmentVariable("MOCK_CLIENTID", "mockClientId");
            Environment.SetEnvironmentVariable("MOCK_USERNAME", "mockUsername");
            Environment.SetEnvironmentVariable("MOCK_USERPASS", "mockPassword");
            Environment.SetEnvironmentVariable("MOCK_CLIENTSECRET", "mockClientSecret");

            _basicAuth = new BasicAuthentication(_authSettings);
            _oauth2Auth = new OAuth2Authentication(_authSettings);
        }

        [Fact]
        public void BasicAuth_NoUsername_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_USERNAME", "");
            Assert.Throws<NullReferenceException>(() => new BasicAuthentication(_authSettings));
        }

        [Fact]
        public void BasicAuth_NoPassword_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_USERPASS", "");
            Assert.Throws<NullReferenceException>(() => new BasicAuthentication(_authSettings));
        }

        [Fact]
        public void BasicAuth_BeforeRequest_TokenGetsAdded()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            _basicAuth.BeforeRequest(request);

            IEnumerable<string> result = request.Headers.GetValues("Authorization");
            foreach (string header in result)
            {
                Assert.Equal("Basic bW9ja1VzZXJuYW1lOm1vY2tQYXNzd29yZA==", header);
            }
        }

        [Fact]
        public void OAuth2_NoUsername_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_USERNAME", "");
            Assert.Throws<NullReferenceException>(() => new OAuth2Authentication(_authSettings));
        }

        [Fact]
        public void OAuth2_NoPassword_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_USERPASS", "");
            Assert.Throws<NullReferenceException>(() => new OAuth2Authentication(_authSettings));
        }

        [Fact]
        public void OAuth2_NoClientID_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_CLIENTID", "");
            Assert.Throws<NullReferenceException>(() => new OAuth2Authentication(_authSettings));
        }

        [Fact]
        public void OAuth2_NoClientSecret_Throws()
        {
            Environment.SetEnvironmentVariable("MOCK_CLIENTSECRET", "");
            Assert.Throws<NullReferenceException>(() => new OAuth2Authentication(_authSettings));
        }

        [Fact]
        public void OAuth2_BeforeRequest_NoTokenIfNotAuthenticated()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            _oauth2Auth.BeforeRequest(request);

            Assert.False(request.Headers.TryGetValues("Authorization", out var result));
        }

        [Fact]
        public async Task OAuth2_BeforeRequest_TokenAddedIfAuthenticated()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            await _oauth2Auth.Authenticate(new MockOAuth2HttpMessageHandler());
            _oauth2Auth.BeforeRequest(request);

            IEnumerable<string> result = request.Headers.GetValues("Authorization");
            foreach (string header in result)
            {
                Assert.Equal("token mockToken", header);
            }
        }

        public class MockOAuth2HttpMessageHandler : HttpMessageHandler
        {
            public bool ReturnSuccess = true;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage();

                httpResponseMessage.StatusCode = ReturnSuccess ? System.Net.HttpStatusCode.Accepted : System.Net.HttpStatusCode.NotFound;

                Dictionary<string, string> jsonResponse = new Dictionary<string, string>();
                jsonResponse.Add("token", "mockToken");
                httpResponseMessage.Content = new StringContent(JsonConvert.SerializeObject(jsonResponse));

                return Task<HttpResponseMessage>.Factory.StartNew(() => httpResponseMessage);
            }
        }
    }
}
