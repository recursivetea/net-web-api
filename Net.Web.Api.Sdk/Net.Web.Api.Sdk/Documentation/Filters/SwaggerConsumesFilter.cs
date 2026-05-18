using System.Linq;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters
{
    /// <summary>
    /// Swagger operation filter that sets the consumes content types from <see cref="SwaggerConsumesAttribute"/>.
    /// </summary>
    public class SwaggerConsumesFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var attribute = context.MethodInfo.GetCustomAttributes(typeof(SwaggerConsumesAttribute), true)
                .Concat(context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(SwaggerConsumesAttribute), true) ?? System.Array.Empty<object>())
                .Cast<SwaggerConsumesAttribute>()
                .FirstOrDefault();

            if (attribute == null) return;

            operation.RequestBody ??= new OpenApiRequestBody();
            operation.RequestBody.Content.Clear();

            foreach (var contentType in attribute.ContentTypes)
            {
                operation.RequestBody.Content[contentType] = new OpenApiMediaType();
            }
        }
    }
}
