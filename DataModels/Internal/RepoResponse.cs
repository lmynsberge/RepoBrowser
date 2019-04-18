namespace DataModels.Internal
{
    /// <summary>
    /// Base Repo Reponse class to be inherited and to provide the base with
    /// </summary>
    public class RepoResponse
    {
        /// <summary>
        /// Can be used in lieu of attempting to cast.
        /// </summary>
        /// <value>The type of the repo response.</value>
        public virtual string RepoResponseType { get; set; }
    }
}
