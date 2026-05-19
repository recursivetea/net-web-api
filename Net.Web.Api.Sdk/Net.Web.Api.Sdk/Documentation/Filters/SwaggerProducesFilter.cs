using System.Linq;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters
{
    /// <summary>
    /// Swagger operation filter that sets the produces content types from <see cref="SwaggerProducesAttribute"/>.
    /// </summary>
    public class SwaggerProducesFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo.GetCustomAttributes(typeof(SwaggerProducesAttribute), true)
                .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(SwaggerProducesAttribute), true) ?? System.Array.Empty<object>())
                .Cast<SwaggerProducesAttribute>()
                .FirstOrDefault();

            if (attribute == null) return;

            foreach (var response in operation.Responses.Values)
            {
                var existingContent = response.Content.Keys.ToList();
                foreach (var key in existingContent)
                    response.Content.Remove(key);

                foreach (var contentType in attribute.ContentTypes)
                    response.Content[contentType] = new OpenApiMediaType();
            }
        }
    }
}
