using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ServiceComposer.AspNetCore
{
    public static class EndpointsExtensions
    {
        static string GetComponentTemplate(Type componentType)
        {
            MethodInfo method = null;
            if (typeof(IHandleRequests).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod("Handle");
            }
            else if (typeof(ISubscribeToCompositionEvents).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod("Subscribe");
            }

            return method?.GetCustomAttribute<HttpGetAttribute>()?.Template;
        }

        public static void MapCompositionHandlers(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var compositionMetadataRegistry = endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .Select(componentType => new
                {
                    ComponentType = componentType,
                    Template = GetComponentTemplate(componentType)
                })
                .Where(component => component.Template != null)
                .GroupBy(component => component.Template);

            foreach (var getComponentsGroup in getComponentsGroupedByTemplate)
            {
                var builder = new CompositionEndpointBuilder(
                    RoutePatternFactory.Parse(getComponentsGroup.Key),
                    getComponentsGroup.Select(component => component.ComponentType),
                    0)
                {
                    DisplayName = getComponentsGroup.Key,
                };
                builder.Metadata.Add(new HttpMethodMetadata(new[] {HttpMethods.Get}));

                var attributes = getComponentsGroup.SelectMany(component => component.ComponentType.GetCustomAttributes());
                foreach (var attribute in attributes)
                {
                    builder.Metadata.Add(attribute);
                }


                var dataSource = endpoints.DataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
                if (dataSource == null)
                {
                    dataSource = new CompositionEndpointDataSource();
                    endpoints.DataSources.Add(dataSource);
                }

                dataSource.AddEndpointBuilder(builder);
            }
        }
    }

    class CompositionEndpointBuilder : EndpointBuilder
    {
        private readonly Type[] _compositionHandlers;
        public RoutePattern RoutePattern { get; set; }

        public int Order { get; set; }

        public CompositionEndpointBuilder(RoutePattern routePattern, IEnumerable<Type> compositionHandlers, int order)
        {
            _compositionHandlers = compositionHandlers.ToArray();
            RoutePattern = routePattern;
            Order = order;
            RequestDelegate = context =>
            {
                var types = _compositionHandlers;
                context.RequestServices.GetRequiredService(types[0]);

                return Task.CompletedTask;
            };
        }

        public override Endpoint Build()
        {
            var routeEndpoint = new RouteEndpoint(
                RequestDelegate,
                RoutePattern,
                Order,
                new EndpointMetadataCollection(Metadata),
                DisplayName);

            return routeEndpoint;
        }
    }

    class CompositionEndpointDataSource : EndpointDataSource
    {
        readonly List<CompositionEndpointBuilder> _endpointBuilders = new List<CompositionEndpointBuilder>();

        public void AddEndpointBuilder(CompositionEndpointBuilder endpointBuilder)
        {
            _endpointBuilders.Add(endpointBuilder);
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpointBuilders
            .OrderBy(builder=>builder.Order)
            .Select(builder => builder.Build()).ToArray();
    }
}