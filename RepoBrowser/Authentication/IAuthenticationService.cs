using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RepoBrowser.Authentication
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated();
        Task Authenticate(HttpMessageHandler handler);
        void BeforeRequest(HttpRequestMessage httpRequest);
        void AfterResponse(HttpResponseMessage httpResponse);
    }
}
