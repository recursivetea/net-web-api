using System.Linq;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Net.Web.Api.Sdk.Documentation.Constants;
using Net.Web.Api.Sdk.Security.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters
{
    /// <summary>
    /// Swagger operation filter that adds security type descriptions from <see cref="SwaggerSecurityTypeAttribute"/>.
    /// </summary>
    public class SwaggerSecurityTypeAttributeFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var securityType = GetSecurityType(context);

            if (!string.IsNullOrEmpty(securityType))
            {
                operation.Description = securityType;
            }
            else
            {
                var attr = context.MethodInfo.GetCustomAttributes(typeof(SwaggerSecurityTypeAttribute), true)
                    .Cast<SwaggerSecurityTypeAttribute>()
                    .FirstOrDefault();

                if (attr != null)
                    operation.Description = attr.SecurityType;
            }
        }

        private static string? GetSecurityType(OperationFilterContext context)
        {
            var methodInfo = context.MethodInfo;
            var controllerType = methodInfo.DeclaringType;

            if (controllerType == null) return null;

            // Check method-level attributes first
            if (methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true).Any())
                return SwaggerSecurityTypeConstants.ANONYMOUS;

            var tokenAttr = methodInfo.GetCustomAttributes(typeof(TokenAuthorizeAttribute), true)
                .Cast<TokenAuthorizeAttribute>()
                .FirstOrDefault();

            if (tokenAttr != null)
                return SwaggerSecurityTypeConstants.TOKEN_SECURED + GetIntendedAudiences(tokenAttr);

            if (methodInfo.GetCustomAttributes(typeof(BasicAuthorizeAttribute), true).Any())
                return SwaggerSecurityTypeConstants.BASIC_SECURED;

            // Check controller-level attributes
            if (controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true).Any())
                return SwaggerSecurityTypeConstants.ANONYMOUS;

            var controllerTokenAttr = controllerType.GetCustomAttributes(typeof(TokenAuthorizeAttribute), true)
                .Cast<TokenAuthorizeAttribute>()
                .FirstOrDefault();

            if (controllerTokenAttr != null)
                return SwaggerSecurityTypeConstants.TOKEN_SECURED + GetIntendedAudiences(controllerTokenAttr);

            return SwaggerSecurityTypeConstants.ANONYMOUS;
        }

        private static string GetIntendedAudiences(TokenAuthorizeAttribute attr)
        {
            return string.IsNullOrEmpty(attr.IntendedAudiences)
                ? string.Empty
                : $"<br/>Intended Audience(s): {attr.IntendedAudiences}";
        }
    }
}
