#if NETCOREAPP3_1 || NET5_0

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
        static Dictionary<string, Type[]> compositionOverControllerGetComponents = new Dictionary<string, Type[]>();
        static Dictionary<string, Type[]> compositionOverControllerPostComponents = new Dictionary<string, Type[]>();

        public static void MapCompositionHandlers(this IEndpointRouteBuilder endpoints)
        {
            MapCompositionHandlers(endpoints, false);
        }

        [Obsolete("To enable write support use the EnableWriteSupport() method on the ViewModelCompositionOptions. This method will be treated as an error in v2 and removed in v3.")]
        public static void MapCompositionHandlers(this IEndpointRouteBuilder endpoints, bool enableWriteSupport)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var options = endpoints.ServiceProvider.GetRequiredService<ViewModelCompositionOptions>();
            if (options.CompositionOverControllersOptions.IsEnabled)
            {
                var compositionOverControllersRoutes = endpoints.ServiceProvider.GetRequiredService<CompositionOverControllersRoutes>();
                compositionOverControllersRoutes.AddGetComponentsSource(compositionOverControllerGetComponents);
                compositionOverControllersRoutes.AddPostComponentsSource(compositionOverControllerPostComponents);
            }

            var compositionMetadataRegistry = endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();

            MapGetComponents(compositionMetadataRegistry, endpoints.DataSources, options.CompositionOverControllersOptions);
            if (enableWriteSupport || options.IsWriteSupportEnabled)
            {
                MapPostComponents(compositionMetadataRegistry, endpoints.DataSources, options.CompositionOverControllersOptions);
                MapPutComponents(compositionMetadataRegistry, endpoints.DataSources, options.CompositionOverControllersOptions);
                MapPatchComponents(compositionMetadataRegistry, endpoints.DataSources, options.CompositionOverControllersOptions);
                MapDeleteComponents(compositionMetadataRegistry, endpoints.DataSources, options.CompositionOverControllersOptions);
            }
        }

        private static void MapGetComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources, CompositionOverControllersOptions compositionOverControllersOptions)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpGetAttribute>(compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out var endpoint))
                {
                    var componentTypes = componentsGroup.Select(c => c.ComponentType).ToArray();
                    compositionOverControllerGetComponents[componentsGroup.Key] = componentTypes;
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup, new HttpMethodMetadata(new[] {HttpMethods.Get}));
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        private static void MapPostComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources, CompositionOverControllersOptions compositionOverControllersOptions)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPostAttribute>(compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out var endpoint))
                {
                    var componentTypes = componentsGroup.Select(c => c.ComponentType).ToArray();
                    compositionOverControllerPostComponents[componentsGroup.Key] = componentTypes;
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup, new HttpMethodMetadata(new[] {HttpMethods.Post}));
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        private static void MapPatchComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources, CompositionOverControllersOptions compositionOverControllersOptions)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPatchAttribute>(compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup, new HttpMethodMetadata(new[] {HttpMethods.Patch}));

                AppendToDataSource(dataSources, builder);
            }
        }

        private static void MapPutComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources, CompositionOverControllersOptions compositionOverControllersOptions)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPutAttribute>(compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup, new HttpMethodMetadata(new[] {HttpMethods.Put}));

                AppendToDataSource(dataSources, builder);
            }
        }

        private static void MapDeleteComponents(CompositionMetadataRegistry compositionMetadataRegistry, ICollection<EndpointDataSource> dataSources, CompositionOverControllersOptions compositionOverControllersOptions)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpDeleteAttribute>(compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup, new HttpMethodMetadata(new[] {HttpMethods.Delete}));

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

        private static bool ThereIsAlreadyAnEndpointForTheSameTemplate(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup, ICollection<EndpointDataSource> dataSources,
            bool useCaseInsensistiveRouteMatching, out Endpoint endpoint)
        {
            foreach (var dataSource in dataSources)
            {
                if (dataSource.GetType() == typeof(CompositionEndpointDataSource))
                {
                    continue;
                }

                endpoint = dataSource.Endpoints.OfType<RouteEndpoint>()
                    .SingleOrDefault(e =>
                    {
                        var rawTemplate = useCaseInsensistiveRouteMatching
                            ? e.RoutePattern.RawText.ToLowerInvariant()
                            : e.RoutePattern.RawText;
                        return rawTemplate == componentsGroup.Key;
                    });

                return endpoint != null;
            }

            endpoint = null;
            return false;
        }

        private static CompositionEndpointBuilder CreateCompositionEndpointBuilder(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup, HttpMethodMetadata methodMetadata)
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

        static IEnumerable<IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)>> SelectComponentsGroupedByTemplate<TAttribute>(
            CompositionMetadataRegistry compositionMetadataRegistry, bool useCaseInsensistiveRouteMatching) where TAttribute : HttpMethodAttribute
        {
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .Select<Type, (Type ComponentType, MethodInfo Method, string Template)>(componentType =>
                {
                    var method = ExtractMethod(componentType);
                    var template = method.GetCustomAttribute<TAttribute>()?.Template.TrimStart('/');
                    if (template != null && useCaseInsensistiveRouteMatching)
                    {
                        template = template.ToLowerInvariant();
                    }

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