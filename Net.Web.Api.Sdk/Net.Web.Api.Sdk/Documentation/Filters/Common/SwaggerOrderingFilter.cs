using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters.Common
{
    /// <summary>
    /// Abstract base class for Swagger document ordering filters.
    /// </summary>
    public abstract class SwaggerOrderingFilter : IDocumentFilter
    {
        /// <inheritdoc />
        public virtual void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OrderingApply(swaggerDoc, context);
        }

        internal static void OrderingApply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc.Paths == null || !swaggerDoc.Paths.Any()) return;

            var tagGroups = new Dictionary<string, IList<ApiOrder>>();

            foreach (var path in swaggerDoc.Paths)
            {
                var tag = GetPrimaryTag(path.Value);
                var order = GetApiOrder(path.Key, path.Value, context);

                if (!tagGroups.ContainsKey(tag))
                    tagGroups[tag] = new List<ApiOrder>();

                tagGroups[tag].Add(new ApiOrder
                {
                    Order = order,
                    PathKey = path.Key,
                    PathValue = path.Value,
                    OperationName = tag
                });

                tagGroups[tag] = tagGroups[tag].OrderBy(c => c.Order).ToList();
            }

            var list = new List<ApiOrder>();
            foreach (var tagGroup in tagGroups)
                list.AddRange(tagGroup.Value);

            var sorted = list.OrderBy(c => c.OperationName).ToList();

            swaggerDoc.Paths.Clear();
            foreach (var item in sorted)
                swaggerDoc.Paths.Add(item.PathKey, item.PathValue);
        }

        internal static string GetPrimaryTag(OpenApiPathItem pathItem)
        {
            var operation = pathItem.Operations.Values.FirstOrDefault();
            return operation?.Tags?.FirstOrDefault()?.Name ?? string.Empty;
        }

        internal static int GetApiOrder(string pathKey, OpenApiPathItem pathItem, DocumentFilterContext context)
        {
            var operation = pathItem.Operations.Values.FirstOrDefault();
            if (operation == null) return -1;

            var operationId = operation.OperationId;
            if (string.IsNullOrEmpty(operationId)) return -1;

            var apiDescription = context.ApiDescriptions
                .FirstOrDefault(d => d.ActionDescriptor.RouteValues.Values.Contains(operationId) ||
                                     (d.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad &&
                                      cad.ActionName == operationId));

            if (apiDescription == null) return -1;

            if (apiDescription.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor)
            {
                var attr = descriptor.MethodInfo.GetCustomAttributes(typeof(SwaggerMethodOrderAttribute), false)
                    .Cast<SwaggerMethodOrderAttribute>()
                    .FirstOrDefault();

                return attr?.Order ?? -1;
            }

            return -1;
        }

        internal class ApiOrder
        {
            internal int Order { get; set; }
            internal string PathKey { get; set; } = string.Empty;
            internal OpenApiPathItem PathValue { get; set; } = new OpenApiPathItem();
            internal string OperationName { get; set; } = string.Empty;
        }
    }
}
