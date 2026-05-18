using System;
using System.IO;
using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Net.Web.Api.Sdk.Common.Constants;
using Net.Web.Api.Sdk.Common.Validations;
using Net.Web.Api.Sdk.Documentation.Filters;
using Net.Web.Api.Sdk.Injection.Containers;
using Net.Web.Api.Sdk.Injection.Installers;
using Net.Web.Api.Sdk.Interfaces.Token;
using Net.Web.Api.Sdk.Security.Handlers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Net.Web.Api.Sdk.Initialization
{
    /// <summary>
    /// Extension methods for registering and configuring the SDK in an ASP.NET Core application.
    /// </summary>
    public static class WebApplicationExtensions
    {
        #region Private Constants

        private const string KIT_DEFAULT_TOKEN_CONFIGURATION = "token-sdk.config";

        #endregion

        #region IServiceCollection Extensions

        /// <summary>
        /// Registers all SDK services into the DI container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblyNamePrefix">Optional assembly name prefix for service discovery.</param>
        public static IServiceCollection AddWebApiSdk(this IServiceCollection services, string? assemblyNamePrefix = null)
        {
            services.AddHttpContextAccessor();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            services.AddControllers(options =>
            {
                options.Filters.Add<ParameterValidationActionFilterAttribute>();
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;
                options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local;
                options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            });

            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddSwaggerGen(ConfigureSwagger);
            services.AddSwaggerGenNewtonsoftSupport();

            // Register SDK services via reflection
            var installer = new ServiceInstaller(assemblyNamePrefix);
            installer.Install(services);

            return services;
        }

        #endregion

        #region IApplicationBuilder Extensions

        /// <summary>
        /// Configures the SDK middleware pipeline.
        /// </summary>
        public static IApplicationBuilder UseWebApiSdk(this IApplicationBuilder app)
        {
            IdentityModelEventSource.ShowPII = true;

            app.UseCors();

            app.UseMiddleware<JwtTokenMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", "v1");
            });

            // Extract default token config to disk
            var assembly = Assembly.GetExecutingAssembly();
            ExtractEmbeddedResource(assembly, EmbeddedResourceConstants.SECURITY_ASSEMBLY_NAMESPACE,
                KIT_DEFAULT_TOKEN_CONFIGURATION, KIT_DEFAULT_TOKEN_CONFIGURATION);

            // Set up the injection container for non-DI contexts
            var serviceProvider = app.ApplicationServices;
            InjectionContainer.Instance.SetServiceProvider(serviceProvider);

            // Cleanup token database on startup
            using var scope = serviceProvider.CreateScope();
            var tokenService = scope.ServiceProvider.GetService<IJwtTokenService>();
            tokenService?.CleanupTokenDatabase();

            return app;
        }

        #endregion

        #region Private Methods

        private static void ConfigureSwagger(SwaggerGenOptions options)
        {
            options.OperationFilter<SwaggerConsumesFilter>();
            options.OperationFilter<SwaggerProducesFilter>();
            options.OperationFilter<SwaggerUploadOperationFilter>();
            options.OperationFilter<SwaggerSecurityTypeAttributeFilter>();

            options.DocumentFilter<SwaggerMethodOrderingFilter>();
            options.DocumentFilter<SwaggerOperationOrderingFilter>();

            options.EnableAnnotations();

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var xmlFiles = Directory.GetFiles(basePath, "doc-api-*.xml");

            foreach (var file in xmlFiles)
            {
                options.IncludeXmlComments(file);
            }
        }

        private static void ExtractEmbeddedResource(Assembly assembly, string nameSpace, string source, string destination)
        {
            var sourceResource = $"{nameSpace}.{source}";
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;

            using var stream = assembly.GetManifestResourceStream(sourceResource);

            if (stream == null) return;

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            var fileName = Path.Combine(rootPath, destination);
            File.WriteAllText(fileName, content);
        }

        #endregion
    }
}
