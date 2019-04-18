using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DataModels.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RepoBrowser.Authentication;
using RepoBrowser.Endpoints;
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

        public static async Task<PullRequestResponse> GetPullRequests(PullRequestRequest pullRequest, IRepoBrowser repoBrowser)
        {
            IAuthenticationService authService = repoBrowser.GetAuthenticationService();
            ITransformationService transformationService = repoBrowser.GetTransformationService();
            HttpMessageHandler httpMessageHandler = repoBrowser.GetHttpMessageHandler();
            
            if (authService != null)
            {
                // First check authentication
                if (!authService.IsAuthenticated())
                {
                    await authService.Authenticate(httpMessageHandler);
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
                if (!transformationService.BeforeRequest(pullRequest, httpRequest)) { return prResponse; }

                // Call the authentication transformation
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


                repeatRequest = transformationService.AfterResponse(httpResponse, prResponse);
            }

            return prResponse;

        }

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

        internal static T CreateObjectOfType<T>(string fullyQualifiedName, params object[] args)
        {
            Type type = Type.GetType(fullyQualifiedName);
            return (T)Activator.CreateInstance(type, args);
        }
    }
}
