using System.Net.Http;
using System.Threading.Tasks;

namespace RepoBrowser.Authentication
{
    /// <summary>
    /// Authentication service interface.
    /// </summary>
    public interface IAuthenticationService
    {
        bool IsAuthenticated();
        Task Authenticate(HttpMessageHandler handler);
        void BeforeRequest(HttpRequestMessage httpRequest);
        void AfterResponse(HttpResponseMessage httpResponse);
    }
}
