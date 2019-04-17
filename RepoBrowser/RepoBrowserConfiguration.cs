using System;
using System.Collections.Generic;
using DataModels.Internal;

namespace RepoBrowser
{
    public class RepoBrowserConfiguration
    {
        public RepositoryType TypeName { get; set; }
        public AuthenticationSettings AuthSettings { get; set; }
        public string AuthType { get; set; }
        public string TransformType { get; set; }
        public string HttpMessageHandlerType { get; set; }
    }

    public class AuthenticationSettings
    {
        public List<string> Scopes { get; set; } = new List<string>();
        public string Note { get; set; }
        public string EnvUserName { get; set; }
        public string EnvUserPassword { get; set; }
        public string EnvClientID { get; set; }
        public string EnvClientSecret { get; set; }
        public string OAuth2Endpoint { get; set; }

    }
}
