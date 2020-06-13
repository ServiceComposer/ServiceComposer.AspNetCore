using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public static class EndpointsExtensions
    {
        static string ExtractComponentTemplate<TAttribute>(Type componentType) where TAttribute : HttpMethodAttribute
        {
            MethodInfo method = null;
            if (typeof(ICompositionRequestsHandler).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod(nameof(ICompositionRequestsHandler.Handle));
            }
            else if (typeof(ICompositionEventsSubscriber).IsAssignableFrom(componentType))
            {
                method = componentType.GetMethod(nameof(ICompositionEventsSubscriber.Subscribe));
            }

            return method?.GetCustomAttribute<TAttribute>()?.Template;
        }

        public static void MapCompositionHandlers(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var compositionMetadataRegistry = endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();
            MapGetComponents(compositionMetadataRegistry, endpoints.DataSources);
        }

        private static void MapGetComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpGetAttribute>(compositionMetadataRegistry);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,new HttpMethodMetadata(new[] {HttpMethods.Get}));

                AppendToDataSource(dataSources, builder);
            }
        }

        private static void AppendToDataSource(ICollection<EndpointDataSource> dataSources, CompositionEndpointBuilder builder)
        {
            var dataSource = dataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = new CompositionEndpointDataSource();
                dataSources.Add(dataSource);
            }

            dataSource.AddEndpointBuilder(builder);
        }

        private static CompositionEndpointBuilder CreateCompositionEndpointBuilder(IGrouping<string, (Type ComponentType, string Template)> componentsGroup, HttpMethodMetadata methodMetadata)
        {
            var builder = new CompositionEndpointBuilder(
                RoutePatternFactory.Parse(componentsGroup.Key),
                componentsGroup.Select(component => component.ComponentType),
                0)
            {
                DisplayName = componentsGroup.Key,
            };
            builder.Metadata.Add(methodMetadata);

            var attributes = componentsGroup.SelectMany(component => component.ComponentType.GetCustomAttributes());
            foreach (var attribute in attributes)
            {
                builder.Metadata.Add(attribute);
            }

            return builder;
        }

        private static IEnumerable<IGrouping<string, (Type ComponentType, string Template)>> SelectComponentsGroupedByTemplate<TAttribute>(CompositionMetadataRegistry compositionMetadataRegistry) where TAttribute : HttpMethodAttribute
        {
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .Select<Type, (Type ComponentType, string Template)>(componentType => (componentType, ExtractComponentTemplate<TAttribute>(componentType)))
                .Where(component => component.Template != null)
                .GroupBy(component => component.Template);
            
            return getComponentsGroupedByTemplate;
        }
    }
}