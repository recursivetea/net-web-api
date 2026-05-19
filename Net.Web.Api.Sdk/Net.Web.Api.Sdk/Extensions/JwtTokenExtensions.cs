using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Net.Web.Api.Sdk.Extensions
{
    /// <summary>
    /// Extension methods for JWT token extraction from HTTP requests.
    /// </summary>
    public static class JwtTokenExtensions
    {
        #region Public Extensions

        /// <summary>
        /// Gets the JWT token from the HTTP request Authorization header.
        /// </summary>
        public static string? GetToken(this HttpRequest request, out JwtSecurityToken? securityToken)
        {
            securityToken = null;
            var authHeader = request.Headers.Authorization.ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            var rawToken = authHeader.Substring("Bearer ".Length).Trim();
            return rawToken.GetToken(out securityToken);
        }

        /// <summary>
        /// Gets the JWT token from the HTTP request Authorization header.
        /// </summary>
        public static string? GetToken(this HttpRequest request)
        {
            return request.GetToken(out _);
        }

        /// <summary>
        /// Parses a raw token string into a JwtSecurityToken.
        /// </summary>
        public static string? GetToken(this string rawToken, out JwtSecurityToken? securityToken)
        {
            securityToken = null;

            if (string.IsNullOrEmpty(rawToken))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                securityToken = tokenHandler.ReadToken(rawToken) as JwtSecurityToken;
                return rawToken;
            }
            catch
            {
                try
                {
                    rawToken = rawToken.FromSecuredEncoded64Padding();
                    rawToken = Encoding.UTF8.GetString(Convert.FromBase64String(rawToken));
                    securityToken = tokenHandler.ReadToken(rawToken) as JwtSecurityToken;
                    return rawToken;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the expiration date of a JWT token string.
        /// </summary>
        public static DateTime GetExpirationDate(this string token)
        {
            if (string.IsNullOrEmpty(token))
                return DateTime.MinValue;

            GetToken(token, out var jwt);

            return jwt?.ValidTo ?? DateTime.MinValue;
        }

        /// <summary>
        /// Determines whether the claims indicate a one-time-use token.
        /// </summary>
        public static bool IsTokenOneTimeUse(this System.Collections.Generic.List<System.Security.Claims.Claim> claims)
        {
            if (claims == null || !claims.Any())
                return false;

            var oneTimeUseValue = claims.GetClaimByName(Net.Web.Api.Sdk.Models.Token.TokenInternalClaimNames.otu.ToString())?.Value;
            return !string.IsNullOrEmpty(oneTimeUseValue) && bool.TryParse(oneTimeUseValue, out var value) && value;
        }

        #endregion
    }
}
