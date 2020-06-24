using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        readonly CompositionMetadataRegistry _compositionMetadataRegistry = new CompositionMetadataRegistry();
#if NETCOREAPP3_1
        readonly CompositionOverControllersRoutes _compositionOverControllersRoutes = new CompositionOverControllersRoutes();
#endif
        
        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
            AssemblyScanner = new AssemblyScanner();

            Services.AddSingleton(_compositionMetadataRegistry);
            
#if NETCOREAPP3_1
            Services.AddSingleton(_compositionOverControllersRoutes);
#endif
        }

        List<(Func<Type, bool>, Action<IEnumerable<Type>>)> typesRegistrationHandlers = new List<(Func<Type, bool>, Action<IEnumerable<Type>>)>();
        Dictionary<Type, Action<Type, IServiceCollection>> configurationHandlers = new Dictionary<Type, Action<Type, IServiceCollection>>();

        public void AddServicesConfigurationHandler(Type serviceType, Action<Type, IServiceCollection> configurationHandler)
        {
            if (configurationHandlers.ContainsKey(serviceType))
            {
                throw new NotSupportedException($"There is already a Services configuration handler for the {serviceType}.");
            }

            configurationHandlers.Add(serviceType, configurationHandler);
        }

        public void AddTypesRegistrationHandler(Func<Type, bool> typesFilter, Action<IEnumerable<Type>> registrationHandler)
        {
            typesRegistrationHandlers.Add((typesFilter, registrationHandler));
        }

        internal void InitializeServiceCollection()
        {
#if NETCOREAPP3_1
            Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
            {
                options.Filters.Add(typeof(CompositionOverControllersActionFilter));
            });
#endif
            
            if (AssemblyScanner.IsEnabled)
            {
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                            && !typeInfo.IsAbstract
                            && typeof(IInterceptRoutes).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterRouteInterceptor(type);
                        }
                    });

                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && (typeof(ICompositionRequestsHandler).IsAssignableFrom(type) || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type));
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterCompositionHandler(type);
                        }
                    });

                var assemblies = AssemblyScanner.Scan();
                var allTypes = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .Distinct()
                    .ToList();

                var optionsCustomizations = allTypes.Where(t => !t.IsAbstract && typeof(IViewModelCompositionOptionsCustomization).IsAssignableFrom(t));
                foreach (var optionsCustomization in optionsCustomizations)
                {
                    var oc = (IViewModelCompositionOptionsCustomization)Activator.CreateInstance(optionsCustomization);
                    oc.Customize(this);
                }

                foreach (var (typesFilter, registrationHandler) in typesRegistrationHandlers)
                {
                    var filteredTypes = allTypes.Where(typesFilter);
                    registrationHandler(filteredTypes);
                }
            }
        }

        public AssemblyScanner AssemblyScanner { get; private set; }

        public IServiceCollection Services { get; private set; }

        public void RegisterRequestsHandler<T>() where T: IHandleRequests
        {
            RegisterRouteInterceptor(typeof(T));
        }

        public void RegisterCompositionEventsSubscriber<T>() where T : ISubscribeToCompositionEvents
        {
            RegisterRouteInterceptor(typeof(T));
        }

        public void RegisterCompositionHandler<T>()
        {
            RegisterCompositionHandler(typeof(T));
        }

        void RegisterCompositionHandler(Type type)
        {
            if (!(typeof(ICompositionRequestsHandler).IsAssignableFrom(type) || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type)))
            {
                throw new NotSupportedException("Registered types must be ICompositionRequestsHandler or ICompositionEventsSubscriber.");
            }

            _compositionMetadataRegistry.AddComponent(type);
            if (configurationHandlers.TryGetValue(type, out var handler))
            {
                handler(type, Services);
            }
            else
            {
                Services.AddTransient(type);
            }
        }

        void RegisterRouteInterceptor(Type type)
        {
            if (configurationHandlers.TryGetValue(type, out var handler))
            {
                handler(type, Services);
            }
            else
            {
                Services.AddTransient(typeof(IInterceptRoutes), type);
            }
        }
    }
}