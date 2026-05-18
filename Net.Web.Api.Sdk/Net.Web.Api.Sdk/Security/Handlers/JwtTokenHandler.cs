using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Net.Web.Api.Sdk.Extensions;
using Net.Web.Api.Sdk.Interfaces.Token;

namespace Net.Web.Api.Sdk.Security.Handlers
{
    /// <summary>
    /// Middleware that validates JWT tokens from the Authorization header and sets the current principal.
    /// </summary>
    public class JwtTokenMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenMiddleware"/> class.
        /// </summary>
        public JwtTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, IJwtTokenService tokenService)
        {
            try
            {
                var token = context.Request.GetToken(out var securityToken);

                if (!string.IsNullOrEmpty(token) && securityToken != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var principal = tokenHandler.ValidateToken(token, tokenService.GetTokenValidationParameters(), out _);
                    context.User = principal;
                }
            }
            catch
            {
                // ignored - invalid tokens are handled by authorization filters
            }

            await _next(context);
        }
    }
}
