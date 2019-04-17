using System;
using Microsoft.AspNetCore.Http;

namespace RepoBrowser.Authentication
{
    public class BasicAuthentication : IAuthenticationService
    {
        public BasicAuthentication()
        {
        }

        public void AfterResponse(HttpResponse httpResponse)
        {
            throw new NotImplementedException();
        }

        public void Authenticate()
        {
            throw new NotImplementedException();
        }

        public void BeforeRequest(HttpRequest httpRequest)
        {
            throw new NotImplementedException();
        }

        public void Configure(Microsoft.Extensions.Options.IOptions options)
        {
            throw new NotImplementedException();
        }

        public bool IsAuthenticated()
        {
            throw new NotImplementedException();
        }
    }
}
