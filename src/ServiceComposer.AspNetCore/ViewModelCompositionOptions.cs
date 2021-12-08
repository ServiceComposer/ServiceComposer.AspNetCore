using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        readonly CompositionMetadataRegistry _compositionMetadataRegistry = new CompositionMetadataRegistry();
        readonly CompositionOverControllersRoutes _compositionOverControllersRoutes = new CompositionOverControllersRoutes();

        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
            AssemblyScanner = new AssemblyScanner();

            Services.AddSingleton(this);
            Services.AddSingleton(_compositionMetadataRegistry);
            ResponseSerialization = new ResponseSerializationOptions(Services);
        }

        internal Func<Type, bool> TypesFilter { get; set; } = _ => true;

        readonly List<(Func<Type, bool>, Action<IEnumerable<Type>>)> typesRegistrationHandlers = new();
        readonly Dictionary<Type, Action<Type, IServiceCollection>> configurationHandlers = new();

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

        internal void InitializeServiceCollection()
        {
            if (CompositionOverControllersOptions.IsEnabled)
            {
                Services.AddSingleton(_compositionOverControllersRoutes);
                Services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(typeof(CompositionOverControllersActionFilter));
                });
            }

            Services.AddSingleton(container =>
            {
                var modelBinderFactory = container.GetService<IModelBinderFactory>();
                var modelMetadataProvider = container.GetService<IModelMetadataProvider>();
                var mvcOptions = container.GetService<IOptions<MvcOptions>>();

                if (modelBinderFactory == null || modelMetadataProvider == null || mvcOptions == null)
                {
                    throw new InvalidOperationException("Unable to resolve one of the services required to support model binding. " +
                                                        "Make sure the application is configured to use MVC services by calling either " +
                                                        $"services.{nameof(MvcServiceCollectionExtensions.AddControllers)}(), or " +
                                                        $"services.{nameof(MvcServiceCollectionExtensions.AddControllersWithViews)}(), or " +
                                                        $"services.{nameof(MvcServiceCollectionExtensions.AddMvc)}(), or " +
                                                        $"services.{nameof(MvcServiceCollectionExtensions.AddRazorPages)}().");
                }

                return new RequestModelBinder(modelBinderFactory, modelMetadataProvider, mvcOptions);
            });

            if (AssemblyScanner.IsEnabled)
            {
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                            && !typeInfo.IsAbstract
#pragma warning disable 618
                            && typeof(IInterceptRoutes).IsAssignableFrom(type);
#pragma warning restore 618
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
                    Debug.Assert(oc != null, nameof(oc) + " != null");
                    oc.Customize(this);
                }

                foreach (var (typesFilter, registrationHandler) in typesRegistrationHandlers)
                {
                    var filteredTypes = allTypes.Where(typesFilter);
                    registrationHandler(filteredTypes);
                }
            }
        }

        public AssemblyScanner AssemblyScanner { get; }

        public IServiceCollection Services { get; }

        public ResponseSerializationOptions ResponseSerialization { get; }

#pragma warning disable 618
        public void RegisterRequestsHandler<T>() where T: IHandleRequests
#pragma warning restore 618
        {
            RegisterRouteInterceptor(typeof(T));
        }

#pragma warning disable 618
        public void RegisterCompositionEventsSubscriber<T>() where T : ISubscribeToCompositionEvents
#pragma warning restore 618
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
                    || typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type)
                )
            )
            {
                var message = $"Registered types must be either {nameof(ICompositionRequestsHandler)}, " +
                              $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";

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

            var globalFactoryRegistration = Services.SingleOrDefault(sd => sd.ServiceType == typeof(IViewModelFactory));
            if (globalFactoryRegistration != null)
            {
                var message = $"Only one global {nameof(IViewModelFactory)} is supported.";
                if (globalFactoryRegistration.ImplementationType != null)
                {
                    message += $" {globalFactoryRegistration.ImplementationType.Name} is already registered as a global view model factory.";
                }

                throw new NotSupportedException(message);
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

        void RegisterRouteInterceptor(Type type)
        {
            if (configurationHandlers.TryGetValue(type, out var handler))
            {
                handler(type, Services);
            }
            else
            {
#pragma warning disable 618
                Services.AddTransient(typeof(IInterceptRoutes), type);
#pragma warning restore 618
            }
        }
    }
}