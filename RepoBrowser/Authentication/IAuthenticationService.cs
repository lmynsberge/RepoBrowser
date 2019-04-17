using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace RepoBrowser.Authentication
{
    public interface IAuthenticationService
    {
        void Configure(IOptions options);
        bool IsAuthenticated();
        void Authenticate();
        void BeforeRequest(HttpRequest httpRequest);
        void AfterResponse(HttpResponse httpResponse);
    }
}
