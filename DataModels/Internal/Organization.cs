using System;
using System.Collections.Generic;

namespace DataModels.Internal
{
    public class Organization
    {
        public int ID { get; set; }
        public OrgRepository Repository { get; set; }

        public Organization() { }
        public Organization(int id)
        {
            ID = id;
        }

        public class OrgRepository
        {
            public string Name { get; set; }
            public RepositoryType Type { get; set; }
        }
    }

    /// <summary>
    /// Organization Repository Type
    /// </summary>
    /// <remarks>Bitbucket is for illustration purposes only and is not supported.</remarks>
    public enum RepositoryType
    {
        Github,
        Bitbucket
    }
}
