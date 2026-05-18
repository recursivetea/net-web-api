using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Net.Web.Api.Sdk.Injection.Attributes;
using Net.Web.Api.Sdk.Models.Token;

namespace Net.Web.Api.Sdk.Interfaces.Token
{
    /// <summary>
    /// Interface IJwtTokenService
    /// </summary>
    [InjectInterfaceService]
    public interface IJwtTokenService
    {
        /// <summary>
        /// Gets the tokens.
        /// </summary>
        Dictionary<string, JwtTokenModel> Tokens { get; }

        /// <summary>
        /// Creates the token.
        /// </summary>
        string CreateToken(string tokenName, string identityName, Dictionary<string, string>? customClaims = null);

        /// <summary>
        /// Gets the token validation parameters.
        /// </summary>
        TokenValidationParameters GetTokenValidationParameters(bool validateExipration = false, string? issuers = null, string? audiences = null);

        /// <summary>
        /// Gets the token payload from the current HTTP context.
        /// </summary>
        Dictionary<string, string> GetTokenPayload(HttpContext context);

        /// <summary>
        /// Gets the identity payload from the current HTTP context.
        /// </summary>
        Dictionary<string, string> GetIdentityPayload(HttpContext context);

        /// <summary>
        /// Determines whether the token is revoked.
        /// </summary>
        bool IsTokenRevoked(string token, List<Claim> claims);

        /// <summary>
        /// Determines whether the token has been used.
        /// </summary>
        bool IsTokenUsed(string token, List<Claim> claims);

        /// <summary>
        /// Revokes the token.
        /// </summary>
        bool RevokeToken(string token, List<Claim> claims);

        /// <summary>
        /// Marks the token as used.
        /// </summary>
        void MarkTokenAsUsed(string token, List<Claim> claims);

        /// <summary>
        /// Cleanups the token database.
        /// </summary>
        int CleanupTokenDatabase();
    }
}
