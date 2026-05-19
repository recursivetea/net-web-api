using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Net.Web.Api.Sdk.Extensions
{
    /// <summary>
    /// Extension methods for ASP.NET Core controllers.
    /// </summary>
    public static class ControllerExtensions
    {
        #region Public Extensions

        /// <summary>
        /// Gets the claims from the current controller's user identity.
        /// </summary>
        public static IList<Claim>? GetClaims(this ControllerBase controller)
        {
            var identity = controller.HttpContext.User?.Identity;

            if (identity == null || !identity.IsAuthenticated)
                return null;

            return ((ClaimsIdentity)identity).Claims.ToList();
        }

        #endregion
    }
}
