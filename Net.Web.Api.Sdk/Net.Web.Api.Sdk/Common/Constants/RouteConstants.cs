namespace Net.Web.Api.Sdk.Common.Constants
{
    /// <summary>
    /// Class RouteConstants.
    /// </summary>
    public static class RouteConstants
    {
        #region Public Constants

        /// <summary>
        /// The route prefix version template for ASP.NET Core attribute routing.
        /// </summary>
        public const string ROUTE_PREFIX_VERSION = "api/v{version:apiVersion}";

        #endregion

        #region Internal Constants

        /// <summary>
        /// The API version field name.
        /// </summary>
        internal const string API_VERSION_FIELD = "apiVersion";

        #endregion
    }
}
