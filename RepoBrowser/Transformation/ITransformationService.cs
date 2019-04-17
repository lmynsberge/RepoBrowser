using System;
using Microsoft.AspNetCore.Http;
using DataModels.Internal;

namespace RepoBrowser.Transformation
{
    public interface ITransformationService
    {
        // Implementations must take an internal data model and turn it into an HTTPRequest
        HttpRequest BeforeRequest(RepoRequest repoRequest);
        // Implementations must take an HTTPResponse and return a RepoResponse object
        RepoResponse AfterResponse(HttpResponse httpResponse);
    }
}
