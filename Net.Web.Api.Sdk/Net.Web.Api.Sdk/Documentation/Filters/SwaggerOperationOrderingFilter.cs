using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Net.Web.Api.Sdk.Documentation.Attributes;
using Net.Web.Api.Sdk.Documentation.Filters.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Documentation.Filters
{
    /// <summary>
    /// Swagger document filter that orders operations by their <see cref="SwaggerOperationOrderAttribute"/> tags.
    /// </summary>
    public class SwaggerOperationOrderingFilter : SwaggerOrderingFilter
    {
        /// <inheritdoc />
        public override void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (swaggerDoc.Paths == null || !swaggerDoc.Paths.Any())
            {
                base.Apply(swaggerDoc, context);
                return;
            }

            var allOperationTags = GetOperationOrder(context);

            if (allOperationTags == null || allOperationTags.Length == 0)
            {
                OrderingApply(swaggerDoc, context);
                return;
            }

            var groups = new Dictionary<int, IDictionary<string, OpenApiPathItem>>();

            foreach (var path in swaggerDoc.Paths)
            {
                var tag = GetPrimaryTag(path.Value);
                var position = allOperationTags.ToList().IndexOf(tag);

                if (position == -1) position = int.MaxValue;

                if (!groups.ContainsKey(position))
                    groups[position] = new Dictionary<string, OpenApiPathItem>();

                groups[position][path.Key] = path.Value;
            }

            groups = groups.OrderBy(c => c.Key).ToDictionary(c => c.Key, c => c.Value);

            swaggerDoc.Paths.Clear();
            foreach (var group in groups)
                foreach (var api in group.Value)
                    swaggerDoc.Paths.Add(api.Key, api.Value);
        }

        private static string[]? GetOperationOrder(DocumentFilterContext context)
        {
            var attributes = new List<SwaggerOperationOrderAttribute>();

            foreach (var apiDescription in context.ApiDescriptions)
            {
                if (apiDescription.ActionDescriptor is not Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor)
                    continue;

                var controllerType = descriptor.ControllerTypeInfo;

                var attr = controllerType.GetCustomAttributes(typeof(SwaggerOperationOrderAttribute), true)
                    .Concat(controllerType.BaseType?.GetCustomAttributes(typeof(SwaggerOperationOrderAttribute), true) ?? System.Array.Empty<object>())
                    .Cast<SwaggerOperationOrderAttribute>()
                    .FirstOrDefault();

                if (attr != null && !attributes.Any(a => a.From == attr.From))
                    attributes.Add(attr);
            }

            if (!attributes.Any()) return null;

            var sdk = attributes.FirstOrDefault(c => c.From == SwaggerOperationOrderAttribute.OperationFrom.Sdk);
            var application = attributes.FirstOrDefault(c => c.From == SwaggerOperationOrderAttribute.OperationFrom.Application);

            if (application == null && sdk != null) return sdk.OperationTags;
            if (sdk == null && application != null) return application.OperationTags;
            if (application == null) return null;

            var result = sdk!.OperationTags.ToList();
            foreach (var tag in application.OperationTags)
                if (!result.Contains(tag))
                    result.Add(tag);

            return result.ToArray();
        }
    }
}
