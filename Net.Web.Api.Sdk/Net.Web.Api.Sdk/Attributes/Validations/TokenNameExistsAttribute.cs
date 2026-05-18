using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Net.Web.Api.Sdk.Interfaces.Token;
using Net.Web.Api.Sdk.Properties;

namespace Net.Web.Api.Sdk.Attributes.Validations
{
    /// <summary>
    /// Validates that the token name exists in the configured token definitions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TokenNameExistsAttribute : ValidationAttribute
    {
        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var service = validationContext.GetService<IJwtTokenService>();

            if (service == null)
                return ValidationResult.Success;

            var tokenName = value?.ToString()?.Trim().ToUpper() ?? string.Empty;
            var exists = service.Tokens.ContainsKey(tokenName);

            return exists
                ? ValidationResult.Success
                : new ValidationResult(string.Format(Resources.TokenNameNotFound, tokenName));
        }
    }
}
