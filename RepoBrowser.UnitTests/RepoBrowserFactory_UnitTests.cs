using System;
using DataModels.Internal;
using RepoBrowser;
using Xunit;

namespace RepoBrowser.UnitTests
{
    public class RepoBrowserFactory_UnitTests
    {
        private Organization _gitHubOrg;
        private Organization _bitBucketOrg;
        private Organization _bothOrg;

        RepoBrowserFactory_UnitTests()
        {
            _gitHubOrg = new Organization(1);
            _gitHubOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _gitHubOrg.Repositories.Add(new Organization.Repository()
            {
                Type = RepositoryType.Github,
                Name = "unittestGH"
            });

            _bitBucketOrg = new Organization(2);
            _bitBucketOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _bitBucketOrg.Repositories.Add(new Organization.Repository()
            {
                Type = RepositoryType.Bitbucket,
                Name = "unittestGH"
            });

            _bothOrg = new Organization(3);
            _bothOrg.Repositories = new System.Collections.Generic.List<Organization.Repository>();
            _bothOrg.Repositories.Add(_gitHubOrg.Repositories[0]);
            _bothOrg.Repositories.Add(_bitBucketOrg.Repositories[0]);
        }

        [Fact]
        public void CreateSearchRepository_GH_RepoCreated()
        {
            IRepoBrowser searchRepo = RepoBrowserFactory.CreateSearchRepository(_gitHubOrg);

        }

        [Fact]
        public void CreateSearchRepository_NoBitBucket_ThrowsException()
        {

        }

        [Fact]
        public void CreateSearchRepository_BothBitBucketGH_ThrowsException()
        {

        }
    }
}
