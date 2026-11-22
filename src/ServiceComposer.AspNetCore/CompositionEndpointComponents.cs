using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// The components of a composition endpoint: the handlers, route-scoped subscribers, and view model factory
    /// of its route. Composition endpoints carry it as endpoint metadata.
    /// </summary>
    public sealed class CompositionEndpointComponents
    {
        internal CompositionEndpointComponents(IReadOnlyList<CompositionComponent> components)
        {
            Components = components;
        }

        public IReadOnlyList<CompositionComponent> Components { get; }
    }

    /// <summary>
    /// The kind of a composition component.
    /// </summary>
    public enum CompositionComponentKind
    {
        RequestsHandler,
        EventsSubscriber,
        ViewModelFactory
    }

    /// <summary>
    /// A component of a composition endpoint.
    /// </summary>
    public sealed class CompositionComponent
    {
        CompositionComponent(Type componentType, Type userType, CompositionComponentKind kind, MethodInfo method)
        {
            ComponentType = componentType;
            UserType = userType;
            Kind = kind;
            Method = method;
            TypeAttributes = userType.GetCustomAttributes(inherit: true);
            MethodAttributes = method.GetCustomAttributes(inherit: true);
        }

        /// <summary>
        /// The registered type. For contract-less composition handlers, it's the generated handler.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// The user's class, also for contract-less composition handlers.
        /// </summary>
        public Type UserType { get; }

        public CompositionComponentKind Kind { get; }

        /// <summary>
        /// The user's method: Handle, Subscribe, or CreateViewModel, or the method of a contract-less composition handler.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// The attributes of <see cref="UserType"/>.
        /// </summary>
        public IReadOnlyList<object> TypeAttributes { get; }

        /// <summary>
        /// The attributes of <see cref="Method"/>.
        /// </summary>
        public IReadOnlyList<object> MethodAttributes { get; }

        internal static CompositionComponent From(Type componentType)
        {
            var generated = componentType.GetCustomAttribute<GeneratedCompositionHandlerAttribute>(inherit: false);
            if (generated != null)
            {
                return new CompositionComponent(componentType, generated.UserType, CompositionComponentKind.RequestsHandler,
                    FindUserMethod(componentType, generated));
            }

            // The same precedence as when registering components
            if (typeof(ICompositionRequestsHandler).IsAssignableFrom(componentType))
            {
                return new CompositionComponent(componentType, componentType, CompositionComponentKind.RequestsHandler,
                    componentType.GetMethod(nameof(ICompositionRequestsHandler.Handle)));
            }

            if (typeof(ICompositionEventsSubscriber).IsAssignableFrom(componentType))
            {
                return new CompositionComponent(componentType, componentType, CompositionComponentKind.EventsSubscriber,
                    componentType.GetMethod(nameof(ICompositionEventsSubscriber.Subscribe)));
            }

            return new CompositionComponent(componentType, componentType, CompositionComponentKind.ViewModelFactory,
                componentType.GetMethod(nameof(IEndpointScopedViewModelFactory.CreateViewModel)));
        }

        // Overloads of a contract-less handler's method are told apart by their routes, which the generated
        // handler's Handle method copies.
        static MethodInfo FindUserMethod(Type componentType, GeneratedCompositionHandlerAttribute generated)
        {
            var candidates = generated.UserType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name == generated.MethodName)
                .ToArray();
            if (candidates.Length == 1)
            {
                return candidates[0];
            }

            var routes = Routes(componentType.GetMethod(nameof(ICompositionRequestsHandler.Handle)));
            return candidates.Single(method => Routes(method).SetEquals(routes));
        }

        static HashSet<(string, string)> Routes(MethodInfo method) =>
            method.GetCustomAttributes<HttpMethodAttribute>(inherit: true)
                .SelectMany(attribute => attribute.HttpMethods.Select(httpMethod => (httpMethod, attribute.Template)))
                .ToHashSet();
    }
}
