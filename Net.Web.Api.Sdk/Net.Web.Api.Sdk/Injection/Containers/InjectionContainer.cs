using System;
using Microsoft.Extensions.DependencyInjection;

namespace Net.Web.Api.Sdk.Injection.Containers
{
    /// <summary>
    /// Class InjectionContainer. This class cannot be inherited.
    /// Provides a singleton accessor to the service provider for use in non-DI contexts.
    /// </summary>
    public sealed class InjectionContainer
    {
        #region Singleton

        private static readonly Lazy<InjectionContainer> _lazy = new Lazy<InjectionContainer>(() => new InjectionContainer());

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static InjectionContainer Instance => _lazy.Value;

        #endregion

        #region Private Properties

        private IServiceProvider? _serviceProvider;

        #endregion

        #region Constructors

        private InjectionContainer() { }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the service provider.
        /// </summary>
        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        public T? GetService<T>() where T : class
        {
            return _serviceProvider?.GetService<T>();
        }

        #endregion
    }
}
