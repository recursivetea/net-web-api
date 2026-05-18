using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Net.Web.Api.Sdk.Security.Attributes
{
    /// <summary>
    /// Abstract base class for Basic authentication authorization filters.
    /// </summary>
    public abstract class BasicAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        #region Internal Constants

        internal const string AUTHORIZATION_BASIC = "Basic";
        internal const string REALM = "realm";

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether to send a WWW-Authenticate challenge.
        /// </summary>
        public bool EnableChallenge { get; set; }

        /// <summary>
        /// Gets or sets the realm.
        /// </summary>
        public string? Realm { get; set; }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Authenticates the user with the provided credentials.
        /// </summary>
        protected abstract Task<IPrincipal?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken, IList<string> authenticationResult);

        #endregion

        #region IAsyncAuthorizationFilter Implementation

        /// <inheritdoc />
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            var authHeader = request.Headers.Authorization.ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            if (!authHeader.StartsWith(AUTHORIZATION_BASIC + " ", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new StatusCodeResult(403);
                return;
            }

            var parameter = authHeader.Substring(AUTHORIZATION_BASIC.Length + 1).Trim();

            if (string.IsNullOrEmpty(parameter))
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            var userNameAndPassword = ExtractUserNameAndPassword(parameter);

            if (userNameAndPassword == null)
            {
                context.Result = new StatusCodeResult(401);
                return;
            }

            var authenticationResult = new List<string>();
            var principal = await AuthenticateAsync(userNameAndPassword.Item1, userNameAndPassword.Item2,
                context.HttpContext.RequestAborted, authenticationResult);

            if (principal == null)
            {
                if (EnableChallenge)
                {
                    var challengeParam = string.IsNullOrEmpty(Realm) ? null : $@"{REALM}=""{Realm}""";
                    var challenge = string.IsNullOrEmpty(challengeParam)
                        ? AUTHORIZATION_BASIC
                        : $"{AUTHORIZATION_BASIC} {challengeParam}";
                    context.HttpContext.Response.Headers.WWWAuthenticate = challenge;
                }
                context.Result = new UnauthorizedObjectResult(authenticationResult);
            }
            else
            {
                context.HttpContext.User = (System.Security.Claims.ClaimsPrincipal)principal;
            }
        }

        #endregion

        #region Private Methods

        private static Tuple<string, string>? ExtractUserNameAndPassword(string authorizationParameter)
        {
            byte[] credentialBytes;

            try
            {
                credentialBytes = Convert.FromBase64String(authorizationParameter);
            }
            catch (FormatException)
            {
                return null;
            }

            var encoding = Encoding.ASCII;
            encoding = (Encoding)encoding.Clone();
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;

            string decodedCredentials;

            try
            {
                decodedCredentials = encoding.GetString(credentialBytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }

            if (string.IsNullOrEmpty(decodedCredentials)) return null;

            var colonIndex = decodedCredentials.IndexOf(':');

            if (colonIndex == -1) return null;

            var userName = decodedCredentials.Substring(0, colonIndex);
            var password = decodedCredentials.Substring(colonIndex + 1);

            return new Tuple<string, string>(userName, password);
        }

        #endregion
    }
}
