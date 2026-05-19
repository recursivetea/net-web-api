using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Net.Web.Api.Sdk.Common.Http
{
    /// <summary>
    /// An IActionResult that adds a WWW-Authenticate challenge header on 401 responses.
    /// </summary>
    public class ChallengeOnUnauthorizedResult : IActionResult
    {
        #region Properties

        /// <summary>
        /// Gets the challenge scheme.
        /// </summary>
        public string ChallengeScheme { get; }

        /// <summary>
        /// Gets the inner result.
        /// </summary>
        public IActionResult InnerResult { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ChallengeOnUnauthorizedResult"/> class.
        /// </summary>
        public ChallengeOnUnauthorizedResult(string challengeScheme, IActionResult innerResult)
        {
            ChallengeScheme = challengeScheme;
            InnerResult = innerResult;
        }

        #endregion

        #region IActionResult Implementation

        /// <inheritdoc />
        public async Task ExecuteResultAsync(ActionContext context)
        {
            await InnerResult.ExecuteResultAsync(context);

            if (context.HttpContext.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                if (!context.HttpContext.Response.Headers.ContainsKey("WWW-Authenticate"))
                {
                    context.HttpContext.Response.Headers.WWWAuthenticate = ChallengeScheme;
                }
            }
        }

        #endregion
    }
}
