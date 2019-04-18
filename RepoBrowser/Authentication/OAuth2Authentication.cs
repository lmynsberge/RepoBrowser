using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RepoBrowser.Authentication
{
    /// <summary>
    /// The OAuth2 implementation for authentication. In my experience, OAuth2 is not consistent everywhere so this would likely require changes or even new classes for
    ///  other services such as BitBucket.
    /// </summary>
    public class OAuth2Authentication : IAuthenticationService
    {
        // fields to get OAuth2 token
        private string _userName;
        private string _password;
        private string _clientId;
        private string _clientSecret;
        private string _authToken;
        private List<string> _scopes;
        private string _note;
        private Uri _oauth2Endpoint;
        private bool _isAuthenticated = false;

        public OAuth2Authentication(AuthenticationSettings authSettings)
        {
            _userName = Environment.GetEnvironmentVariable(authSettings.EnvUserName);
            _password = Environment.GetEnvironmentVariable(authSettings.EnvUserPassword);
            _clientId = Environment.GetEnvironmentVariable(authSettings.EnvClientID);
            _clientSecret = Environment.GetEnvironmentVariable(authSettings.EnvClientSecret);
            _scopes = authSettings.Scopes;
            _note = authSettings.Note;
            _oauth2Endpoint = new Uri(authSettings.OAuth2Endpoint);

            if (string.IsNullOrEmpty(_userName)) { throw new NullReferenceException("Username must not be null."); }
            if (string.IsNullOrEmpty(_password)) { throw new NullReferenceException("Password must not be null."); }
            if (string.IsNullOrEmpty(_clientId)) { throw new NullReferenceException("Client ID must not be null."); }
            if (string.IsNullOrEmpty(_clientSecret)) { throw new NullReferenceException("Client Secret must not be null."); }


        }

        /// <summary>
        /// Not used currently
        /// </summary>
        /// <param name="httpResponse">Http response.</param>
        public void AfterResponse(HttpResponseMessage httpResponse)
        {
            return;
        }

        /// <summary>
        /// Reaches out for the authentication request
        /// </summary>
        public async Task Authenticate(HttpMessageHandler handler = null)
        {
            HttpClient httpClient = new HttpClient(handler);
            HttpRequestMessage requestMessage = new HttpRequestMessage();
            requestMessage.Method = HttpMethod.Post;
            string userNameToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(_userName + ":" + _password));
            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", userNameToken);
            requestMessage.Headers.Add("User-Agent", "RepoBrowserServce");
            requestMessage.RequestUri = _oauth2Endpoint;
            
            // Body is from the OAuth2 settings
            OAuth2RequestObject body = new OAuth2RequestObject(_scopes, _note, _clientId, _clientSecret);
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.SendAsync(requestMessage);

            // Check if it was successful first, otherwise the following code won't work.
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Failed HTTP request for OAuth2 authenticaiton: " + response.StatusCode + " - " + response.Content.ReadAsStringAsync().Result);
            }
            _isAuthenticated = true;
            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(await response.Content.ReadAsStringAsync());
            _authToken = (string)result["token"];
        }

        /// <summary>
        /// Adds in the OAuth2 header
        /// </summary>
        /// <param name="httpRequest">Http request.</param>
        public void BeforeRequest(HttpRequestMessage httpRequest)
        {
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", _authToken);
        }

        /// <summary>
        /// Always return true once Authenticate() is called -- some systems might have timeout periods
        /// </summary>
        /// <returns><c>true</c>, if authenticated <c>false</c> otherwise.</returns>
        public bool IsAuthenticated()
        {
            return _isAuthenticated;
        }

        /// <summary>
        /// The OAuth2 Request object to be serialized.
        /// </summary>
        private class OAuth2RequestObject
        {
            [JsonProperty(PropertyName = "scopes")]
            public List<string> Scopes { get; set; }
            [JsonProperty(PropertyName = "note")]
            public string Note { get; set; }
            [JsonProperty(PropertyName = "client_id")]
            public string ClientID { get; set; }
            [JsonProperty(PropertyName = "client_secret")]
            public string ClientSecret { get; set; }

            public OAuth2RequestObject(List<string> scopes, string note, string clientId, string clientSecret)
            {
                Scopes = scopes;
                // Always a unique Guid for testing
                Note = note + Guid.NewGuid();
                ClientID = clientId;
                ClientSecret = clientSecret;

            }

        }
    }
}
