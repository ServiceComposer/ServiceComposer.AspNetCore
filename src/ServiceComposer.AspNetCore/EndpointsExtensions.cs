using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    public static partial class EndpointsExtensions
    {
        static readonly Dictionary<string, (Type ComponentType, IList<object> Metadata)[]> compositionOverControllerGetComponents = new();
        static readonly Dictionary<string, (Type ComponentType, IList<object> Metadata)[]> compositionOverControllerPostComponents = new();

        public static IEndpointConventionBuilder MapCompositionHandlers(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var options = endpoints.ServiceProvider.GetRequiredService<ViewModelCompositionOptions>();
            options.ResponseSerialization.ValidateConfiguration(endpoints.ServiceProvider
                .GetRequiredService<ILogger<ResponseSerializationOptions>>());

            if (options.CompositionOverControllersOptions.IsEnabled)
            {
                var compositionOverControllersRoutes =
                    endpoints.ServiceProvider.GetRequiredService<CompositionOverControllersRoutes>();
                compositionOverControllersRoutes.AddComponentsSource("get", compositionOverControllerGetComponents);
                compositionOverControllersRoutes.AddComponentsSource("post", compositionOverControllerPostComponents);
            }

            var compositionMetadataRegistry =
                endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();

            var logger = endpoints.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(EndpointsExtensions));

            MapGetComponents(
                compositionMetadataRegistry,
                endpoints.DataSources,
                options.CompositionOverControllersOptions,
                options.ResponseSerialization.DefaultResponseCasing,
                options.ResponseSerialization.UseOutputFormatters,
                logger);
            if (options.IsWriteSupportEnabled)
            {
                MapPostComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters,
                    logger);
                MapPutComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters,
                    logger);
                MapPatchComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters,
                    logger);
                MapDeleteComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters,
                    logger);
            }
            
            var dataSource = endpoints.DataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
            return dataSource;
        }

        static void MapGetComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpGetAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameRoute(
                    componentsGroup, HttpMethods.Get, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out _))
                {
                    compositionOverControllerGetComponents[componentsGroup.Key] = ExtractComponentsWithMetadata(componentsGroup);
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Get}), defaultCasing, useOutputFormatters, logger);
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        static void MapPostComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPostAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameRoute(
                    componentsGroup, HttpMethods.Post, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out _))
                {
                    compositionOverControllerPostComponents[componentsGroup.Key] = ExtractComponentsWithMetadata(componentsGroup);
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Post}), defaultCasing, useOutputFormatters, logger);
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        static void MapPatchComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPatchAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Patch}), defaultCasing, useOutputFormatters, logger);

                AppendToDataSource(dataSources, builder);
            }
        }

        static void MapPutComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPutAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Put}), defaultCasing, useOutputFormatters, logger);

                AppendToDataSource(dataSources, builder);
            }
        }

        static void MapDeleteComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpDeleteAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Delete}), defaultCasing, useOutputFormatters, logger);

                AppendToDataSource(dataSources, builder);
            }
        }

        static void AppendToDataSource(ICollection<EndpointDataSource> dataSources,
            CompositionEndpointBuilder builder)
        {
            var dataSource = dataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = new CompositionEndpointDataSource();
                dataSources.Add(dataSource);
            }

            dataSource.AddEndpointBuilder(builder);
        }

        // Finds the endpoint, e.g. a controller action, already mapped for the same template and
        // HTTP method, in any endpoint data source but the composition ones.
        static bool ThereIsAlreadyAnEndpointForTheSameRoute(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup,
            string httpMethod,
            ICollection<EndpointDataSource> dataSources,
            bool useCaseInsensitiveRouteMatching, out Endpoint endpoint)
        {
            endpoint = dataSources
                .Where(dataSource => dataSource is not CompositionEndpointDataSource)
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .FirstOrDefault(e =>
                {
                    var rawTemplate = useCaseInsensitiveRouteMatching
                        ? e.RoutePattern.RawText?.ToLowerInvariant()
                        : e.RoutePattern.RawText;
                    if (rawTemplate != componentsGroup.Key)
                    {
                        return false;
                    }

                    // Endpoints without HTTP method metadata accept any method
                    var methods = e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
                    return methods == null || methods.Count == 0
                        || methods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase);
                });

            return endpoint != null;
        }

        static (Type ComponentType, IList<object> Metadata)[] ExtractComponentsWithMetadata(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup)
        {
            return componentsGroup
                .Select(c => (c.ComponentType, GetComponentMetadata(c.ComponentType, c.Method)))
                .ToArray();
        }

        // The class' attributes come first, so that, as for controllers,
        // the method's attributes win when endpoint metadata is read.
        static IList<object> GetComponentMetadata(Type componentType, MethodInfo method) =>
            componentType.GetCustomAttributes(inherit: true)
                .Concat(method.GetCustomAttributes(inherit: true))
                .ToArray();

        static CompositionEndpointBuilder CreateCompositionEndpointBuilder(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup,
            HttpMethodMetadata methodMetadata,
            ResponseCasing defaultCasing,
            bool useOutputFormatters,
            ILogger logger)
        {
            var builder = new CompositionEndpointBuilder(
                RoutePatternFactory.Parse(componentsGroup.Key),
                componentsGroup.Select(component => component.ComponentType).ToArray(),
                0,
                defaultCasing,
                useOutputFormatters)
            {
                DisplayName = componentsGroup.Key,
            };
            builder.Metadata.Add(methodMetadata);

            // builds a list of metadata for each component in this endpoint
            foreach (var component in componentsGroup)
            {
                builder.ComponentsMetadata.Add((component.ComponentType, GetComponentMetadata(component.ComponentType, component.Method)));
            }
            
            // Authorization skips the whole endpoint if it has any IAllowAnonymous metadata, so a
            // component allowing anonymous requests would disable the others' authorization. The most
            // restrictive requirements apply instead: IAllowAnonymous is left out if any component
            // requires authorization.
            var componentsRequiringAuthorization = builder.ComponentsMetadata
                .Where(cm => cm.Metadata.OfType<IAuthorizeData>().Any() && !cm.Metadata.OfType<IAllowAnonymous>().Any())
                .Select(cm => cm.ComponentType.Name)
                .ToArray();
            var anyComponentRequiresAuthorization = componentsRequiringAuthorization.Length > 0;
            if (anyComponentRequiresAuthorization)
            {
                var componentsAllowingAnonymous = builder.ComponentsMetadata
                    .Where(cm => cm.Metadata.OfType<IAllowAnonymous>().Any())
                    .Select(cm => cm.ComponentType.Name)
                    .ToArray();
                if (componentsAllowingAnonymous.Length > 0)
                {
                    logger?.LogWarning("[AllowAnonymous] on {ComponentsAllowingAnonymous} is ignored for {HttpMethods} {Template}, " +
                                       "since authorization is required by {ComponentsRequiringAuthorization}: the most restrictive requirements apply to the route.",
                        string.Join(", ", componentsAllowingAnonymous),
                        string.Join(", ", methodMetadata.HttpMethods),
                        componentsGroup.Key,
                        string.Join(", ", componentsRequiringAuthorization));
                }
            }

            // flattens the metadata
            foreach (var metadata in builder.ComponentsMetadata.SelectMany(cm=>cm.Metadata))
            {
                if (anyComponentRequiresAuthorization && metadata is IAllowAnonymous)
                {
                    continue;
                }

                builder.Metadata.Add(metadata);
            }

            return builder;
        }

        static IEnumerable<IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)>>
            SelectComponentsGroupedByTemplate<TAttribute>(
                CompositionMetadataRegistry compositionMetadataRegistry, bool useCaseInsensitiveRouteMatching, ILogger logger)
            where TAttribute : HttpMethodAttribute
        {
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .SelectMany<Type, (Type ComponentType, MethodInfo Method, string Template)>(componentType =>
                {
                    var method = ExtractMethod(componentType);
                    var componentsAndTemplate = method.GetCustomAttributes<TAttribute>()?
                        .Select(a =>
                        {
                            var template = a.Template.TrimStart('/');
                            return (componentType, method, useCaseInsensitiveRouteMatching
                                ? template.ToLowerInvariant()
                                : template);
                        }).ToArray();

                    // var groupsCount = componentsAndTemplate.GroupBy(c => c.Item3).Count();
                    // if (groupsCount != componentsAndTemplate.Length)
                    // {
                    //     throw new NotSupportedException("Multiple attributes of the same type with the same template are not supported.");
                    // }

                    return componentsAndTemplate;
                })
                .Where(component => component.Template != null)
                .GroupBy(component => component.Template)
                .Where(componentsGroup => HasCompositionHandlers<TAttribute>(componentsGroup, logger))
                .ToArray();

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
            else if (typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(componentType))
            {
                return componentType.GetMethod(nameof(IEndpointScopedViewModelFactory.CreateViewModel));
            }

            var message = $"Component needs to be either {nameof(ICompositionRequestsHandler)}, " +
                          $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";
            throw new NotSupportedException(message);
        }

        // A route whose components are only event subscribers or view model factories has nothing
        // to compose: mapping it would make its requests run the composition only to return a 404,
        // and, when composing over a controller, replace the controller response with a 404.
        static bool HasCompositionHandlers<TAttribute>(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup,
            ILogger logger)
            where TAttribute : HttpMethodAttribute
        {
            if (componentsGroup.Any(component => typeof(ICompositionRequestsHandler).IsAssignableFrom(component.ComponentType)))
            {
                return true;
            }

            var httpMethods = componentsGroup.First().Method.GetCustomAttributes<TAttribute>().First().HttpMethods;
            logger?.LogWarning("No composition handler is registered for {HttpMethods} /{Template}, so the route isn't mapped, " +
                               "and these components never run for it: {Components}. Check the template for typos, or add a composition handler for the route.",
                string.Join(", ", httpMethods),
                componentsGroup.Key,
                string.Join(", ", componentsGroup.Select(component => component.ComponentType.Name).Distinct()));

            return false;
        }
    }
}