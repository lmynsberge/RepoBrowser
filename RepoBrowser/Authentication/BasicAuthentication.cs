using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RepoBrowser.Authentication
{
    /// <summary>
    /// Implementation of basic authentication. Fortunately, Basic authentication is implemented pretty similarly across vendors.
    /// It could require some modifications to support certificates or other scenarios.
    /// </summary>
    public class BasicAuthentication : IAuthenticationService
    {

        // encoded username
        private string authToken;

        public BasicAuthentication(AuthenticationSettings authSettings)
        {
            string userName = Environment.GetEnvironmentVariable(authSettings.EnvUserName);
            string password = Environment.GetEnvironmentVariable(authSettings.EnvUserPassword);

            if (string.IsNullOrEmpty(userName)) { throw new NullReferenceException("Username must not be null."); }
            if (string.IsNullOrEmpty(password)) { throw new NullReferenceException("Password must not be null."); }

            // Passwords are assumed ASCII
            authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password));
        }

        /// <summary>
        /// Nothing to do with the response.
        /// </summary>
        /// <param name="responseMessage">Response message.</param>
        public void AfterResponse(HttpResponseMessage responseMessage)
        {
            return;
        }

        /// <summary>
        /// Does nothing for basic authentication since the pieces are just added to the header.
        /// </summary>
        public async Task Authenticate(HttpMessageHandler handler = null)
        {
            return;
        }

        /// <summary>
        /// Adds in the authentication header.
        /// </summary>
        /// <param name="httpRequest">Http request.</param>
        public void BeforeRequest(HttpRequestMessage httpRequest)
        {
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        }

        /// <summary>
        /// Always true since it's just added in.
        /// </summary>
        /// <returns><c>true</c>, if authenticated, <c>false</c> otherwise.</returns>
        public bool IsAuthenticated()
        {
            return true;
        }
    }
}
