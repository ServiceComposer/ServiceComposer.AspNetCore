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
#if NETCOREAPP3_1 || NET5_0
        readonly CompositionOverControllersRoutes _compositionOverControllersRoutes = new CompositionOverControllersRoutes();
#endif

        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
            AssemblyScanner = new AssemblyScanner();

            Services.AddSingleton(this);
            Services.AddSingleton(_compositionMetadataRegistry);
        }

        internal Func<Type, bool> TypesFilter { get; set; } = type => true;

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

#if NETCOREAPP3_1 || NET5_0
        internal CompositionOverControllersOptions CompositionOverControllersOptions { get; private set; } = new CompositionOverControllersOptions();

        public void EnableCompositionOverControllers()
        {
            EnableCompositionOverControllers(false);
        }

        public void EnableCompositionOverControllers(bool useCaseInsensitiveRouteMatching)
        {
            CompositionOverControllersOptions.IsEnabled = true;
            CompositionOverControllersOptions.UseCaseInsensitiveRouteMatching = useCaseInsensitiveRouteMatching;
        }

        internal bool IsWriteSupportEnabled { get; private set; }

        public void EnableWriteSupport()
        {
            IsWriteSupportEnabled = true;
        }
#endif

        internal void InitializeServiceCollection()
        {
#if NETCOREAPP3_1 || NET5_0
            if (CompositionOverControllersOptions.IsEnabled)
            {
                Services.AddSingleton(_compositionOverControllersRoutes);
                Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
                {
                    options.Filters.Add(typeof(CompositionOverControllersActionFilter));
                });
            }
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
                            RegisterCompositionComponents(type);
                        }
                    });

#if NETCOREAPP3_1 || NET5_0
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeof(IViewModelPreviewHandler).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            Services.AddTransient(typeof(IViewModelPreviewHandler), type);
                        }
                    });

                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeof(IViewModelFactory).IsAssignableFrom(type)
                               && !typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterGlobalViewModelFactory(type);
                        }
                    });

                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            Services.AddTransient(typeof(IEndpointScopedViewModelFactory), type);
                        }
                    });
#endif

                var assemblies = AssemblyScanner.Scan();
                var allTypes = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(TypesFilter)
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
            RegisterCompositionComponents(typeof(T));
        }

        void RegisterCompositionComponents(Type type)
        {
            if (
                !(
                    typeof(ICompositionRequestsHandler).IsAssignableFrom(type)
                    || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type)
#if NETCOREAPP3_1 || NET5_0
                    || typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type)
#endif
                )
            )
            {
#if NETCOREAPP3_1 || NET5_0
                var message = $"Registered types must be either {nameof(ICompositionRequestsHandler)}, " +
                              $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";
#else
                var message = $"Registered types must be either {nameof(ICompositionRequestsHandler)} " +
                              $"or {nameof(ICompositionEventsSubscriber)}.";
#endif
                throw new NotSupportedException(message);
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

#if NETCOREAPP3_1 || NET5_0
        public void RegisterEndpointScopedViewModelFactory<T>() where T: IEndpointScopedViewModelFactory
        {
            RegisterCompositionComponents(typeof(T));
        }

        public void RegisterGlobalViewModelFactory<T>() where T: IViewModelFactory
        {
            RegisterGlobalViewModelFactory(typeof(T));
        }

        void RegisterGlobalViewModelFactory(Type viewModelFactoryType)
        {
            if (viewModelFactoryType == null)
            {
                throw new ArgumentNullException(nameof(viewModelFactoryType));
            }

            if (!typeof(IViewModelFactory).IsAssignableFrom(viewModelFactoryType))
            {
                throw new ArgumentOutOfRangeException($"Type must implement {nameof(IViewModelFactory)}.");
            }

            if (typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(viewModelFactoryType))
            {
                var paramName = $"To register {nameof(IEndpointScopedViewModelFactory)} use " +
                                $"the {nameof(RegisterEndpointScopedViewModelFactory)} method.";
                throw new ArgumentOutOfRangeException(paramName);
            }

            if (configurationHandlers.TryGetValue(viewModelFactoryType, out var handler))
            {
                handler(viewModelFactoryType, Services);
            }
            else
            {
                Services.AddTransient(typeof(IViewModelFactory), viewModelFactoryType);
            }
        }
#endif

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