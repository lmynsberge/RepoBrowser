using System;
using System.Collections.Generic;

namespace DataModels.Internal
{
    public class Organization
    {
        public int ID { get; private set; }
        public List<Repository> Repositories = new List<Repository>();

        public Organization(int id)
        {
            ID = id;
        }

        public class Repository
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
