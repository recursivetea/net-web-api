using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using Net.Web.Api.Sdk.Interfaces.Information;
using Net.Web.Api.Sdk.Interfaces.Token;
using Net.Web.Api.Sdk.Properties;

namespace Net.Web.Api.Sdk.Implementations.Information
{
    /// <summary>
    /// Class InformationService.
    /// </summary>
    public sealed class InformationService : IInformationService
    {
        #region Services

        private readonly IJwtTokenService _tokenService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InformationService"/> class.
        /// </summary>
        public InformationService(IJwtTokenService tokenService)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        #endregion

        #region IInformationService Implementations

        /// <summary>
        /// Gets the SDK informations.
        /// </summary>
        public dynamic GetSdkInformations()
        {
            dynamic result = new ExpandoObject();

            result.library = GetAssemblyInformations();

            var tokens = _tokenService.Tokens.Select(c => c.Value).OrderBy(c => c.TokenName);
            result.availableTokens = tokens;

            return result;
        }

        #endregion

        #region Private Methods

        private dynamic GetAssemblyInformations()
        {
            var libraryAssembly = Assembly.GetAssembly(GetType())!;
            var description = libraryAssembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
            var copyright = libraryAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            var title = libraryAssembly.GetCustomAttribute<AssemblyTitleAttribute>();
            var version = libraryAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var author = libraryAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();

            dynamic result = new ExpandoObject();

            result.title = title?.Title ?? string.Empty;
            result.description = description?.Description ?? string.Empty;
            result.version = version?.Version ?? string.Empty;
            result.copyright = copyright?.Copyright ?? string.Empty;
            result.author = author?.Company ?? string.Empty;
            result.license = Resources.License;

            return result;
        }

        #endregion
    }
}
