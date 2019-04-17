using System;
using System.Net.Http;
using DataModels.Internal;

namespace RepoBrowser.Transformation
{
    public class GithubGQLTransformation : ITransformationService
    {
        public GithubGQLTransformation()
        {
        }

        public bool AfterResponse(HttpResponseMessage httpResponse, RepoResponse repoResponse)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRequest(RepoRequest repoRequest, HttpRequestMessage httpRequest)
        {
            throw new NotImplementedException();
        }
    }
}
