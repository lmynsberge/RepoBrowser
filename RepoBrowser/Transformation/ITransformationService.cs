using System;
using System.Net.Http;
using DataModels.Internal;

namespace RepoBrowser.Transformation
{
    public interface ITransformationService
    {
        /// <summary>
        /// Implementations must take an internal data model and turn it into an HTTPRequest
        /// </summary>
        /// <returns><c>true</c>, if request should continue, <c>false</c> otherwise.</returns>
        /// <param name="repoRequest">Repo request.</param>
        /// <param name="httpRequest">Http request.</param>
        bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest);

        /// <summary>
        /// Implementations must take an internal data model and turn it into a RepoResponse object
        /// </summary>
        /// <returns><c>true</c>, if request process should continue <c>false</c> otherwise.</returns>
        /// <param name="httpResponse">Http response.</param>
        /// <param name="repoResponse">Repo response.</param>
        bool AfterResponse(HttpResponseMessage httpResponse, RepoResponse repoResponse);
    }
}
