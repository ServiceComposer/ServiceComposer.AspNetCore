using System;
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
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .Select(componentType => new
                {
                    ComponentType = componentType,
                    Template = ExtractComponentTemplate<HttpGetAttribute>(componentType)
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
}