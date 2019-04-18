using System.Net.Http;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;

namespace RepoBrowser
{
    /// <summary>
    /// A repo browser for Github. Would likely be the same for Bitbucket, but could have small modifications.
    /// </summary>
    public class GithubRepoBrowser : IRepoBrowser
    {
        private IAuthenticationService _authService;
        private ITransformationService _transformService;
        private HttpMessageHandler _httpHandler;

        public GithubRepoBrowser(IAuthenticationService authService, ITransformationService transformService, HttpMessageHandler httpHandler)
        {
            _authService = authService;
            _transformService = transformService;
            _httpHandler = httpHandler;
        }

        public IAuthenticationService GetAuthenticationService()
        {
            return _authService;
        }

        public HttpMessageHandler GetHttpMessageHandler()
        {
            return _httpHandler;
        }

        public ITransformationService GetTransformationService()
        {
            return _transformService;
        }
    }
}
