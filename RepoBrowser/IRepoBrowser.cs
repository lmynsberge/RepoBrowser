using System.Net.Http;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;

namespace RepoBrowser
{
    /// <summary>
    /// Repo browser interface to tightly define its requirements for communication and data gathering.
    /// </summary>
    public interface IRepoBrowser
    {
        IAuthenticationService GetAuthenticationService();
        ITransformationService GetTransformationService();
        HttpMessageHandler GetHttpMessageHandler();
    }
}
