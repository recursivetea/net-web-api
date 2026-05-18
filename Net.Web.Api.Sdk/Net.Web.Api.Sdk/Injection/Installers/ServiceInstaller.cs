using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Net.Web.Api.Sdk.Injection.Attributes;

namespace Net.Web.Api.Sdk.Injection.Installers
{
    /// <summary>
    /// Class ServiceInstaller. Registers services with the ASP.NET Core DI container.
    /// </summary>
    public class ServiceInstaller
    {
        #region Private Properties

        private readonly string? _assemblyNamePrefix;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
        /// </summary>
        /// <param name="assemblyNamePrefix">The assembly name prefix filter.</param>
        public ServiceInstaller(string? assemblyNamePrefix = null)
        {
            _assemblyNamePrefix = assemblyNamePrefix;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Installs services into the <see cref="IServiceCollection"/>.
        /// </summary>
        public void Install(IServiceCollection services)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToList();

            var registrationList = GetRegistrationList(assemblies);

            foreach (var item in registrationList)
            {
                services.AddSingleton(item.Key, item.Value);
            }
        }

        #endregion

        #region Private Methods

        private static IList<Type> GetInterfaces(Type @class)
        {
            return @class.GetInterfaces()
                .Where(@interface => @interface.GetCustomAttribute(typeof(InjectInterfaceServiceAttribute), false) != null)
                .ToList();
        }

        private Dictionary<Type, Type> GetRegistrationList(IEnumerable<Assembly> assemblies)
        {
            var classes = new List<Type>();

            foreach (var assembly in assemblies)
            {
                try
                {
                    classes.AddRange(
                        assembly.GetTypes()
                            .Where(t => t.IsClass && !t.IsAbstract &&
                                (string.IsNullOrEmpty(_assemblyNamePrefix) ||
                                 t.Assembly.FullName!.StartsWith(_assemblyNamePrefix, StringComparison.OrdinalIgnoreCase)))
                    );
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                }
            }

            var registrationList = new List<KeyValuePair<Type, Type>>();

            foreach (var @class in classes)
            {
                if (!string.IsNullOrEmpty(_assemblyNamePrefix) &&
                    !@class.Assembly.FullName!.StartsWith(_assemblyNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var interfaces = GetInterfaces(@class);

                if (interfaces.Count == 0 || interfaces.Count > 2)
                {
                    continue;
                }

                Type? @interface = null;

                if (interfaces.Count == 2)
                {
                    var i1 = interfaces[0];
                    var i2 = interfaces[1];

                    if (i1.GetInterfaces().Any(c => c.FullName == i2.FullName))
                        @interface = i1;
                    else if (i2.GetInterfaces().Any(c => c.FullName == i1.FullName))
                        @interface = i2;
                }
                else
                {
                    @interface = interfaces[0];
                }

                if (@interface == null) continue;

                registrationList.Add(new KeyValuePair<Type, Type>(@interface, @class));
            }

            registrationList = registrationList.OrderBy(c => c.Key.FullName).ToList();

            var result = new Dictionary<Type, Type>();

            foreach (var item in registrationList)
            {
                if (!result.ContainsKey(item.Key))
                {
                    result.Add(item.Key, item.Value);
                    continue;
                }

                var existingClass = result[item.Key];
                var currentClass = item.Value;
                var isExistingCustom = IsCustomService(existingClass);
                var isCurrentCustom = IsCustomService(currentClass);

                if (!isExistingCustom && isCurrentCustom)
                {
                    result[item.Key] = currentClass;
                }
            }

            return result;
        }

        private static bool IsCustomService(Type @class)
        {
            return @class.GetCustomAttributes(typeof(InjectServiceCustomAttribute), false).Any();
        }

        #endregion
    }
}
