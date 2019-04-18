namespace DataModels.Internal
{
    /// <summary>
    /// Base Repo Request class to be inherited and to provide the base with
    /// </summary>
    public class RepoRequest
    {
        /// <summary>
        /// Can be used in lieu of attempting to cast.
        /// </summary>
        /// <value>The type of the repo request.</value>
        public virtual string RepoRequestType { get; set; }
    }
}
