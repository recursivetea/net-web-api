using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Net.Web.Api.Sdk.Extensions;
using Net.Web.Api.Sdk.Interfaces.Token;
using Net.Web.Api.Sdk.Models.Token;

namespace Net.Web.Api.Sdk.Security.Attributes
{
    /// <summary>
    /// Authorization filter that validates JWT tokens.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TokenAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets the issuers (comma-separated).
        /// </summary>
        public string? Issuers { get; set; }

        /// <summary>
        /// Gets or sets the intended audiences (comma-separated).
        /// </summary>
        public string? IntendedAudiences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to validate expiration.
        /// </summary>
        public bool ValidateExpiration { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the token to validate against.
        /// </summary>
        public string? TokenValidatingName { get; set; }

        #endregion

        #region IAuthorizationFilter Implementation

        /// <inheritdoc />
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var identity = context.HttpContext.User?.Identity;

            if (identity == null || !identity.IsAuthenticated)
            {
                context.Result = new ObjectResult(TokenStatus.TokenRequired) { StatusCode = 403 };
                return;
            }

            var token = context.HttpContext.Request.GetToken(out _);

            if (string.IsNullOrEmpty(token))
            {
                context.Result = new ObjectResult(TokenStatus.TokenRequired) { StatusCode = 401 };
                return;
            }

            var claims = ((ClaimsIdentity)identity).Claims.ToList();
            var tokenValidatingName = TokenValidatingName;

            if (string.IsNullOrEmpty(tokenValidatingName))
                tokenValidatingName = claims.GetClaimByName(TokenInternalClaimNames.tn.ToString())?.Value;

            if (string.IsNullOrEmpty(tokenValidatingName))
            {
                context.Result = new ObjectResult(TokenStatus.Invalid) { StatusCode = 401 };
                return;
            }

            var service = context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>();
            var tokenDefinition = service.Tokens.ContainsKey(tokenValidatingName) ? service.Tokens[tokenValidatingName] : null;

            if (tokenDefinition == null)
            {
                context.Result = new ObjectResult(TokenStatus.Invalid) { StatusCode = 401 };
                return;
            }

            try
            {
                var validationParameters = service.GetTokenValidationParameters(ValidateExpiration, Issuers, IntendedAudiences);
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, validationParameters, out _);

                if (service.IsTokenRevoked(token, claims))
                {
                    context.Result = new ObjectResult(TokenStatus.Revoked) { StatusCode = 401 };
                    return;
                }

                if (claims.IsTokenOneTimeUse())
                {
                    if (service.IsTokenUsed(token, claims))
                    {
                        context.Result = new ObjectResult(TokenStatus.AlreadyUsed) { StatusCode = 401 };
                        return;
                    }

                    service.MarkTokenAsUsed(token, claims);
                }
            }
            catch (SecurityTokenExpiredException)
            {
                context.Result = new ObjectResult(TokenStatus.Expired) { StatusCode = 401 };
            }
            catch (SecurityTokenInvalidAudienceException)
            {
                context.Result = new ObjectResult(TokenStatus.InvalidAudience) { StatusCode = 401 };
            }
            catch (Exception ex)
            {
                var isTechnicalError = !(ex.Message.StartsWith("IDX") && ex.Message.Contains(":"));
                context.Result = isTechnicalError
                    ? new ObjectResult(ex.Message) { StatusCode = 500 }
                    : new ObjectResult(TokenStatus.Invalid) { StatusCode = 401 };
            }
        }

        #endregion
    }
}
