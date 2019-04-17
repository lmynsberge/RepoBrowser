using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RepoBrowser.Authentication;
using RepoBrowser.Transformation;
using RepoBrowser.Endpoints;
using System.Net.Http;

namespace RepoBrowser
{
    public interface IRepoBrowser
    {
        IAuthenticationService GetAuthenticationService();
        ITransformationService GetTransformationService();
        HttpMessageHandler GetHttpMessageHandler();

        JsonResult Search(string database, Dictionary<string, string> searchArguments);

        Task<JsonResult> SearchAsync(string database, Dictionary<string, string> searchArguments);
    }
}
