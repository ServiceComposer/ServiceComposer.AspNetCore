using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        readonly CompositionMetadataRegistry compositionMetadataRegistry = new CompositionMetadataRegistry();

        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
            AssemblyScanner = new AssemblyScanner();

            Services.AddSingleton(compositionMetadataRegistry);
        }

        List<(Func<Type, bool>, Action<IEnumerable<Type>>)> typesRegistrationHandlers = new List<(Func<Type, bool>, Action<IEnumerable<Type>>)>();

        public void AddTypesRegistrationHandler(Func<Type, bool> typesFilter, Action<IEnumerable<Type>> registrationHandler)
        {
            typesRegistrationHandlers.Add((typesFilter, registrationHandler));
        }

        internal void InitializeServiceCollection()
        {
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

                var assemblies = AssemblyScanner.Scan();
                var allTypes = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .Distinct();

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
            var type = typeof(T);
            if (!(typeof(ICompositionRequestsHandler).IsAssignableFrom(type) || typeof(ICompositionEventsSubscriber).IsAssignableFrom(type)))
            {
                throw new NotSupportedException("Registered types must be ICompositionRequestsHandler or ICompositionEventsSubscriber.");
            }

            compositionMetadataRegistry.AddComponent(type);
            Services.AddTransient(typeof(T));
        }

        internal void RegisterRouteInterceptor(Type type)
        {
            Services.AddTransient(typeof(IInterceptRoutes), type);
        }
    }
}