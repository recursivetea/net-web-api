using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Net.Web.Api.Sdk.Models.Token;
using Net.Web.Api.Sdk.Properties;

namespace Net.Web.Api.Sdk.Attributes.Validations
{
    /// <summary>
    /// Validates that a token payload collection has valid keys and values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TokenPayloadValidAttribute : ValidationAttribute
    {
        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var name = string.IsNullOrEmpty(validationContext.DisplayName)
                ? validationContext.MemberName
                : validationContext.DisplayName;

            if (value == null) return ValidationResult.Success;

            if (value is not List<KeyValuePair<string, string>> collection)
                return new ValidationResult(string.Format(GetDefaultRequiredMessage(), name));

            if (collection.Count == 0) return ValidationResult.Success;

            var keys = collection.Select(r => r.Key).ToList();

            if (keys.Any(string.IsNullOrEmpty))
                return new ValidationResult(Resources.TokenPayloadEmptyKeyNoAllowed);

            var values = collection.Select(r => r.Value).ToList();

            if (values.Any(string.IsNullOrEmpty))
                return new ValidationResult(Resources.TokenPayloadEmptyValuesAreNotAllowed);

            var duplicated = keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicated.Count > 0)
                return new ValidationResult(string.Format(Resources.TokenPayloadDuplicatedKeys, string.Join(", ", duplicated)));

            var reserved = Enum.GetNames(typeof(TokenInternalClaimNames)).ToList();
            var intersection = reserved.Intersect(keys).ToList();

            if (intersection.Count > 0)
                return new ValidationResult(string.Format(Resources.TokenPayloadReservedKeys, string.Join(", ", intersection)));

            return ValidationResult.Success;
        }

        private static string GetDefaultRequiredMessage()
        {
            var message = string.Empty;
            var assembly = Assembly.GetAssembly(typeof(RequiredAttribute));

            if (assembly != null)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.Name.Equals("DataAnnotationsResources")) continue;

                    var property = type.GetProperty("RequiredAttribute_ValidationError", BindingFlags.NonPublic | BindingFlags.Static);

                    if (property != null)
                    {
                        message = (string?)property.GetValue(null) ?? string.Empty;
                        break;
                    }
                }
            }

            return !string.IsNullOrEmpty(message) ? message : Resources.FieldRequiredText;
        }
    }
}
