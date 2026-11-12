using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ServiceComposer.AspNetCore
{
    public partial class ViewModelCompositionOptions
    {
        readonly IConfiguration _configuration;
        readonly CompositionMetadataRegistry _compositionMetadataRegistry = new CompositionMetadataRegistry();
        readonly CompositionOverControllersRoutes _compositionOverControllersRoutes = new CompositionOverControllersRoutes();

        internal ViewModelCompositionOptions(IServiceCollection services, IConfiguration configuration = null)
        {
            _configuration = configuration;
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

        public void EnableCompositionOverControllers(bool useCaseInsensitiveRouteMatching = true)
        {
            CompositionOverControllersOptions.IsEnabled = true;
            CompositionOverControllersOptions.UseCaseInsensitiveRouteMatching = useCaseInsensitiveRouteMatching;
        }

        internal bool IsWriteSupportEnabled { get; private set; } = true;

        public void DisableWriteSupport()
        {
            IsWriteSupportEnabled = false;
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

            isInitializing = true;

            // Customizations from all sources run before any component is
            // registered, so that, for example, services configuration handlers
            // apply to every component regardless of where it comes from.
            foreach (var componentsProvider in componentsProviders)
            {
                componentsProvider.Customize(this);
            }

            List<Type> allTypes = null;
            if (AssemblyScanner.IsEnabled)
            {
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
                            RegisterEndpointScopedViewModelFactory(type);
                        }
                    });
                
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && !typeof(Attribute).IsAssignableFrom(type) //We don't want to register attributes in DI 
                               && typeof(ICompositionRequestFilter).IsAssignableFrom(type);
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterCompositionRequestsFilter(type);
                        }
                    });
                
                AddTypesRegistrationHandler(
                    typesFilter: type =>
                    {
                        var typeInfo = type.GetTypeInfo();
                        return !typeInfo.IsInterface
                               && !typeInfo.IsAbstract
                               && typeInfo.ImplementedInterfaces.Any(ii => ii.IsGenericType && ii.GetGenericTypeDefinition() == typeof(ICompositionEventsHandler<>));
                    },
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterCompositionEventsHandler(type);
                        }
                    });
                
                AddTypesRegistrationHandler(
                    typesFilter: IsContractlessCompositionHandler,
                    registrationHandler: types =>
                    {
                        foreach (var type in types)
                        {
                            RegisterCompositionComponents(type);
                        }
                    });

                // Assemblies with a registered components provider are skipped:
                // the provider already registers their components and customizations.
                var providersAssemblies = componentsProviders
                    .Select(provider => provider.GetType().Assembly)
                    .ToHashSet();
                var assemblies = AssemblyScanner.Scan();
                allTypes = assemblies
                    .Where(assembly => !providersAssemblies.Contains(assembly))
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
            }

            foreach (var componentsProvider in componentsProviders)
            {
                componentsProvider.RegisterComponents(this);
            }

            if (allTypes != null)
            {
                foreach (var (typesFilter, registrationHandler) in typesRegistrationHandlers)
                {
                    var filteredTypes = allTypes.Where(typesFilter);
                    registrationHandler(filteredTypes);
                }
            }
        }

        readonly List<ICompositionComponentsProvider> componentsProviders = [];
        internal IReadOnlyList<ICompositionComponentsProvider> ComponentsProviders => componentsProviders;
        bool isInitializing;

        /// <summary>
        /// Registers the composition components and the options customizations
        /// that the <typeparamref name="TProvider"/> provider defines.
        /// </summary>
        /// <remarks>
        /// When assembly scanning is enabled, the assembly that defines
        /// <typeparamref name="TProvider"/> is not scanned. Registering the
        /// same provider more than once has no effect.
        /// </remarks>
        public void RegisterComponentsFrom<TProvider>() where TProvider : ICompositionComponentsProvider, new()
        {
            if (isInitializing)
            {
                throw new InvalidOperationException(
                    $"{nameof(RegisterComponentsFrom)} must be called from the {nameof(ServiceCollectionExtensions.AddViewModelComposition)} " +
                    $"configuration callback. It cannot be called from options customizations or components providers.");
            }

            if (componentsProviders.Any(provider => provider.GetType() == typeof(TProvider)))
            {
                return;
            }

            componentsProviders.Add(new TProvider());
        }

        internal void RegisterCompositionRequestsFilter(Type type)
        {
            type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICompositionRequestFilter<>))
                .ToList()
                .ForEach(compositionFilterGenericInterfaceType =>
                {
                    Services.AddTransient(compositionFilterGenericInterfaceType, type); 
                });
        }

        void RegisterCompositionEventsHandler(Type type)
        {
            type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICompositionEventsHandler<>))
                .ToList()
                .ForEach(compositionEventsHandlerGenericInterfaceType =>
                {
                    var eventType = compositionEventsHandlerGenericInterfaceType.GetGenericArguments()[0];
                    // TODO: do we need to support configurationHandlers here?
                    Services.AddTransient(type);
                    _compositionMetadataRegistry.AddEventHandler(eventType, type);
                });
        }

        public AssemblyScanner AssemblyScanner { get; }

        public IServiceCollection Services { get; }

        public IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    throw new ArgumentException("No configuration instance has been set. " +
                                                "To access the application configuration call the " +
                                                "AddViewModelComposition overload te accepts an " +
                                                "IConfiguration instance.");
                }
                return _configuration;
            }
        }

        public ResponseSerializationOptions ResponseSerialization { get; }

        public void RegisterCompositionHandler<T>()
        {
            RegisterCompositionComponents(typeof(T));
        }

        void RegisterCompositionComponents(Type type)
        {
            var didSomething = false;
            if(type.GetInterfaces()
               .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICompositionEventsHandler<>)))
            {
                RegisterCompositionEventsHandler(type);
                didSomething = true;
            }

            var isContractlessCompositionHandler = IsContractlessCompositionHandler(type);
            if (typeof(ICompositionRequestsHandler).IsAssignableFrom(type)
                || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type)
                || typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(type)
                || isContractlessCompositionHandler)
            {
                if (!isContractlessCompositionHandler)
                {
                    // We don't want (yet?) to register contract-less composition handlers
                    // in the metadata registry because they are not a first-class citizen
                    // in the ASP.Net endpoint
                    _compositionMetadataRegistry.AddComponent(type, ExtractComponentMetadata(type));
                }

                if (configurationHandlers.TryGetValue(type, out var handler))
                {
                    handler(type, Services);
                }
                else
                {
                    Services.AddTransient(type);
                }
                didSomething = true;
            }

            if (didSomething == false)
            {
                const string message = $"Registered types must be either {nameof(ICompositionRequestsHandler)}, " +
                                       $"{nameof(ICompositionEventsSubscriber)}, {nameof(ICompositionEventsHandler<SomeEvent>)}, {nameof(IEndpointScopedViewModelFactory)}, or a contract-less composition handler.";

                throw new NotSupportedException(message);
            }
        }

        static IReadOnlyList<object> ExtractComponentMetadata(Type componentType)
        {
            MethodInfo method;
            if (typeof(ICompositionRequestsHandler).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod(nameof(ICompositionRequestsHandler.Handle));
            }
            else if (typeof(ICompositionEventsSubscriber).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod(nameof(ICompositionEventsSubscriber.Subscribe));
            }
            else if (typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod(nameof(IEndpointScopedViewModelFactory.CreateViewModel));
            }
            else
            {
                var message = $"Component needs to be either {nameof(ICompositionRequestsHandler)}, " +
                              $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";
                throw new NotSupportedException(message);
            }

            return method.GetCustomAttributes(inherit: true);
        }

        static bool IsContractlessCompositionHandler(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return !typeInfo.IsInterface
                   && !typeInfo.IsAbstract
                   && type.GetCustomAttributes(typeof(CompositionHandlerAttribute), false).Length > 0;
        }

        void RegisterEndpointScopedViewModelFactory(Type viewModelFactoryType)
        {
            RegisterCompositionComponents(viewModelFactoryType);
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
    }

    class SomeEvent;
}