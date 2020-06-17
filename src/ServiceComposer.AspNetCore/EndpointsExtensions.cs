#if NETCOREAPP3_1

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
        public static void MapCompositionHandlers(this IEndpointRouteBuilder endpoints, bool enableWriteSupport = false)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var compositionMetadataRegistry =
                endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();
            MapGetComponents(compositionMetadataRegistry, endpoints.DataSources);
            if (enableWriteSupport)
            {
                MapPostComponents(compositionMetadataRegistry, endpoints.DataSources);
                MapPutComponents(compositionMetadataRegistry, endpoints.DataSources);
                MapPatchComponents(compositionMetadataRegistry, endpoints.DataSources);
                MapDeleteComponents(compositionMetadataRegistry, endpoints.DataSources);
            }
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

        private static void MapPostComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPostAttribute>(compositionMetadataRegistry);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,new HttpMethodMetadata(new[] {HttpMethods.Post}));

                AppendToDataSource(dataSources, builder);
            }
        }
        
        private static void MapPatchComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPatchAttribute>(compositionMetadataRegistry);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,new HttpMethodMetadata(new[] {HttpMethods.Patch}));

                AppendToDataSource(dataSources, builder);
            }
        }
        
        private static void MapPutComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPutAttribute>(compositionMetadataRegistry);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,new HttpMethodMetadata(new[] {HttpMethods.Put}));

                AppendToDataSource(dataSources, builder);
            }
        }
        
        private static void MapDeleteComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpDeleteAttribute>(compositionMetadataRegistry);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,new HttpMethodMetadata(new[] {HttpMethods.Delete}));

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

        private static CompositionEndpointBuilder CreateCompositionEndpointBuilder(IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup, HttpMethodMetadata methodMetadata)
        {
            var builder = new CompositionEndpointBuilder(
                RoutePatternFactory.Parse(componentsGroup.Key),
                componentsGroup.Select(component => component.ComponentType),
                0)
            {
                DisplayName = componentsGroup.Key,
            };
            builder.Metadata.Add(methodMetadata);

            var attributes = componentsGroup.SelectMany(component => component.Method.GetCustomAttributes());
            foreach (var attribute in attributes)
            {
                builder.Metadata.Add(attribute);
            }

            return builder;
        }

        static IEnumerable<IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)>> SelectComponentsGroupedByTemplate<TAttribute>(CompositionMetadataRegistry compositionMetadataRegistry) where TAttribute : HttpMethodAttribute
        {
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .Select<Type, (Type ComponentType, MethodInfo Method, string Template)>(componentType =>
                {
                    var method = ExtractMethod(componentType);
                    var template = method.GetCustomAttribute<TAttribute>()?.Template;
                    return (componentType, method, template);
                })
                .Where(component => component.Template != null)
                .GroupBy(component => component.Template);

            return getComponentsGroupedByTemplate;
        }

        static MethodInfo ExtractMethod(Type componentType)
        {
            if (typeof(ICompositionRequestsHandler).IsAssignableFrom(componentType))
            {
                return componentType.GetMethod(nameof(ICompositionRequestsHandler.Handle));
            }
            else if (typeof(ICompositionEventsSubscriber).IsAssignableFrom(componentType))
            {
                return componentType.GetMethod(nameof(ICompositionEventsSubscriber.Subscribe));
            }

            throw new NotSupportedException($"Component needs to be either {nameof(ICompositionRequestsHandler)} or {nameof(ICompositionEventsSubscriber)}.");
        }
    }
}

#endif