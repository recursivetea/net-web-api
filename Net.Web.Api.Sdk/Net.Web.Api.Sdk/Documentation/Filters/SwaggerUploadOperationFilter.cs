using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters
{
    /// <summary>
    /// Swagger operation filter that configures file upload operations from <see cref="SwaggerUploadOperationAttribute"/>.
    /// </summary>
    public class SwaggerUploadOperationFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var upload = context.MethodInfo.GetCustomAttributes(typeof(SwaggerUploadOperationAttribute), true)
                .Cast<SwaggerUploadOperationAttribute>()
                .FirstOrDefault();

            if (upload == null) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["fileInformation"] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "File to upload"
                                }
                            },
                            Required = { "fileInformation" }
                        }
                    }
                }
            };
        }
    }
}
