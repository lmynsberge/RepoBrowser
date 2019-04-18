using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DataModels.Internal;
using Microsoft.Extensions.Caching.Memory;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;

namespace RepoBrowser
{
    public static class RepoBrowserFactory
    {
        /// <summary>
        /// Creates the repo browsing interface.
        /// </summary>
        /// <returns>The search repository.</returns>
        /// <param name="organization">Organization.</param>
        /// <param name="config">Config.</param>
        public static IRepoBrowser CreateRepoBrowser(Organization organization, List<RepoBrowserConfiguration> config, IMemoryCache memoryCache)
        {
            if (organization.Repository == null)
            {
                throw new NullReferenceException("Organization must be configured with a repository.");
            }

            switch(organization.Repository.Type)
            {
                case RepositoryType.Github:
                    RepoBrowserConfiguration finalConfig = config.FindLast((obj) => obj.TypeName == RepositoryType.Github);
                    // Get the last instance of the Github repo definitions
                    return GetGithubRepoBrowser(organization, finalConfig, memoryCache);
                default:
                    throw new NotSupportedException("Repository type not currently supported: " + organization.Repository);
            }
        }

        /// <summary>
        /// Gets the pull requests.
        /// </summary>
        /// <returns>The pull requests.</returns>
        /// <param name="pullRequest">Pull request request object.</param>
        /// <param name="repoBrowser">Repo browser.</param>
        public static async Task<PullRequestResponse> GetPullRequests(PullRequestRequest pullRequest, IRepoBrowser repoBrowser)
        {
            IAuthenticationService authService = repoBrowser.GetAuthenticationService();
            ITransformationService transformationService = repoBrowser.GetTransformationService();
            HttpMessageHandler httpMessageHandler = repoBrowser.GetHttpMessageHandler();
            
            if (authService != null)
            {
                try
                {
                    // First check authentication
                    if (!authService.IsAuthenticated())
                    {
                        await authService.Authenticate(httpMessageHandler);
                    }
                }
                catch(Exception ex)
                {
                    throw new InvalidOperationException("During authentication, the custom authentication service threw an exception: " + ex.Message);
                }
            }

            // Make the requests and repeat as necessary by the transformation class
            HttpClient httpClient = new HttpClient(httpMessageHandler);
            HttpResponseMessage httpResponse = null;
            PullRequestResponse prResponse = new PullRequestResponse();

            bool repeatRequest = true;
            while(repeatRequest)
            {
                // Need a new request message every time
                HttpRequestMessage httpRequest = new HttpRequestMessage();
                // Always add User-Agent header by default
                httpRequest.Headers.Add("User-Agent", "RepoBrowserService");

                // Now make any transformations
                try
                {
                    if (!transformationService.BeforeRequest(pullRequest, httpRequest)) { return prResponse; }
                }
                catch(Exception ex)
                {
                    throw new InvalidOperationException("Before making the request the custom transformation service threw an exception: " + ex.Message);
                }

                // Call the authentication transformation
                try
                {
                    if (authService != null)
                    {
                        authService.BeforeRequest(httpRequest);
                        httpResponse = await httpClient.SendAsync(httpRequest);
                        authService.AfterResponse(httpResponse);
                    }
                    else
                    {
                        httpResponse = await httpClient.SendAsync(httpRequest);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("There was an error while making the actual web request: " + ex.Message);
                }

                try
                { 
                    repeatRequest = transformationService.AfterResponse(httpResponse, prResponse);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("After receiving the response the custom transformation service threw an exception: " + ex.Message);
                }
            }

            return prResponse;

        }

        /// <summary>
        /// Computes hash of a string. For use to use as a key in the memory cache.
        /// </summary>
        /// <returns>The hash.</returns>
        /// <param name="textToCompute">Text to compute.</param>
        private static string ComputeHash(string textToCompute)
        {
            // Can use SHA1 - since this is just a quick memory check. 
            using (SHA1 sha1Hasher = SHA1.Create())
            {
                // Get the actual hash 
                byte[] bytes = sha1Hasher.ComputeHash(Encoding.UTF8.GetBytes(textToCompute));

                // Efficiently grab string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Gets the github repo browser internally. Tries to pull the authentication service from memory since that should be a one-time thing.
        /// The transformation service should be separate in case there are multiple requests from the service and messages in flight. We don't want
        /// the result array being shared across requests.
        /// </summary>
        /// <returns>The github repo browser.</returns>
        /// <param name="org">Org.</param>
        /// <param name="config">Config.</param>
        /// <param name="memoryCache">Memory cache.</param>
        internal static IRepoBrowser GetGithubRepoBrowser(Organization org, RepoBrowserConfiguration config, IMemoryCache memoryCache)
        {
            // Authentication can be re-used
            string hashKey = ComputeHash(config.AuthType);
            // First check our cache for the value
            if (memoryCache != null && memoryCache.TryGetValue(hashKey, out IAuthenticationService authService)) 
            {
            }
            else
            {
                authService = CreateObjectOfType<IAuthenticationService>(config.AuthType, config.AuthSettings);
                if (memoryCache != null)
                {
                    memoryCache.Set(hashKey, authService);
                }
            }

            ITransformationService transformService = CreateObjectOfType<ITransformationService>(config.TransformType);
            HttpMessageHandler messageHandler = null;
            if (!string.IsNullOrEmpty(config.HttpMessageHandlerType))
            {
                messageHandler = CreateObjectOfType<HttpMessageHandler>(config.HttpMessageHandlerType);
            }
            else
            {
                messageHandler = new HttpClientHandler();
            }
            return new GithubRepoBrowser(authService, transformService, messageHandler);
        }

        /// <summary>
        /// Wrapper function to create an object based on the FQDN (or assembly + method within this build).
        /// </summary>
        /// <returns>The object of type.</returns>
        /// <param name="fullyQualifiedName">Fully qualified name.</param>
        /// <param name="args">Arguments.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        internal static T CreateObjectOfType<T>(string fullyQualifiedName, params object[] args)
        {
            // Provide a better error in the case where someone configures their own
            try
            {
                Type type = Type.GetType(fullyQualifiedName);
                return (T)Activator.CreateInstance(type, args);
            }
            catch(Exception ex)
            {
                throw new DllNotFoundException(string.Format("The requested class to be used ({0}) could not be generated. More info: " + ex.Message, fullyQualifiedName));
            }

        }
    }
}
