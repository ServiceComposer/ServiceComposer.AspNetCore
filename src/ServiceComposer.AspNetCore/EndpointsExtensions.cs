using System;
using System.Collections.Generic;
using System.Linq;
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
        public static CompositionEndpointConventionBuilder MapCompositionHandlers(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var options = endpoints.ServiceProvider.GetRequiredService<ViewModelCompositionOptions>();
            options.ResponseSerialization.ValidateConfiguration(endpoints.ServiceProvider
                .GetRequiredService<ILogger<ResponseSerializationOptions>>());
            if (options.ResponseSerialization.UseOutputFormatters)
            {
                OutputFormattersServices.EnsureRegistered(endpoints.ServiceProvider,
                    $"{nameof(ViewModelCompositionOptions.ResponseSerialization)}.{nameof(ResponseSerializationOptions.UseOutputFormatters)}");
            }

            var compositionMetadataRegistry =
                endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();

            var logger = endpoints.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(EndpointsExtensions));
            var applicationServices = endpoints.ServiceProvider;

            logger?.LogDebug("{ComponentCount} composition component(s) registered.", compositionMetadataRegistry.Components.Count);

            var dataSource = endpoints.DataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
            if (dataSource == null)
            {
                dataSource = new CompositionEndpointDataSource();
                endpoints.DataSources.Add(dataSource);
            }

            endpoints.ServiceProvider.GetService<CompositionEndpointDataSources>()?.Add(dataSource);

            var dataSources = endpoints.DataSources;
            var compositionOverControllersRoutes =
                endpoints.ServiceProvider.GetRequiredService<CompositionOverControllersRoutes>();
            dataSource.AddDeferredMapping(() =>
            {
                // Finding the controller endpoints that opted in to composition over
                // controllers builds them: it must happen after all conventions are added
                var compositionOverControllersOptions =
                    ResolveCompositionOverControllersOptions(dataSources);

                // Composed routes are per application: a static registry would be
                // shared by every host in the process, e.g. tests running in parallel
                var compositionOverControllerGetComponents = new Dictionary<string, (Type ComponentType, IList<object> Metadata)[]>();
                var compositionOverControllerPostComponents = new Dictionary<string, (Type ComponentType, IList<object> Metadata)[]>();
                if (compositionOverControllersOptions.IsEnabled)
                {
                    compositionOverControllersRoutes.Initialize(compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);
                    compositionOverControllersRoutes.AddComponentsSource("get", compositionOverControllerGetComponents);
                    compositionOverControllersRoutes.AddComponentsSource("post", compositionOverControllerPostComponents);
                }

                MapGetComponents(
                    compositionMetadataRegistry,
                    dataSources,
                    dataSource,
                    compositionOverControllerGetComponents,
                    compositionOverControllersOptions,
                    options.ResponseSerialization.ExplicitResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters,
                    logger,
                    applicationServices);
                if (options.IsWriteSupportEnabled)
                {
                    MapPostComponents(
                        compositionMetadataRegistry,
                        dataSources,
                        dataSource,
                        compositionOverControllerPostComponents,
                        compositionOverControllersOptions,
                        options.ResponseSerialization.ExplicitResponseCasing,
                        options.ResponseSerialization.UseOutputFormatters,
                        logger,
                        applicationServices);
                    MapPutComponents(
                        compositionMetadataRegistry,
                        dataSource,
                        compositionOverControllersOptions,
                        options.ResponseSerialization.ExplicitResponseCasing,
                        options.ResponseSerialization.UseOutputFormatters,
                        logger,
                        applicationServices);
                    MapPatchComponents(
                        compositionMetadataRegistry,
                        dataSource,
                        compositionOverControllersOptions,
                        options.ResponseSerialization.ExplicitResponseCasing,
                        options.ResponseSerialization.UseOutputFormatters,
                        logger,
                        applicationServices);
                    MapDeleteComponents(
                        compositionMetadataRegistry,
                        dataSource,
                        compositionOverControllersOptions,
                        options.ResponseSerialization.ExplicitResponseCasing,
                        options.ResponseSerialization.UseOutputFormatters,
                        logger,
                        applicationServices);
                }
                else
                {
                    WarnAboutUnmappedWriteRoutes(compositionMetadataRegistry, logger);
                }
            });

            return new CompositionEndpointConventionBuilder(dataSource);
        }

        // Write support is enabled by default, so components declaring write routes while it's
        // disabled are most likely a configuration mistake: without this, their routes silently
        // get a 404, or a 405 if a GET composition handler shares the path.
        static void WarnAboutUnmappedWriteRoutes(CompositionMetadataRegistry compositionMetadataRegistry, ILogger logger)
        {
            if (logger == null)
            {
                return;
            }

            var writeComponents = compositionMetadataRegistry.Components
                .Select(component => (Type: component.Key, Attributes: component.Value
                    .OfType<HttpMethodAttribute>()
                    .Where(attribute => attribute is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute)
                    .ToArray()))
                .Where(component => component.Attributes.Length > 0)
                .ToArray();

            if (writeComponents.Length == 0)
            {
                return;
            }

            var unmappedWriteRoutes = writeComponents
                .SelectMany(component => component.Attributes.SelectMany(attribute => attribute.HttpMethods.Select(method =>
                    $"{method} /{RouteTemplates.Normalize(attribute.Template)} ({component.Type.Name})")))
                .Distinct()
                .OrderBy(route => route, StringComparer.Ordinal);

            logger.LogWarning("Write support is disabled, so the write routes of {ComponentCount} composition component(s) aren't mapped, " +
                              "and requests to them get a 404, or a 405 if a GET composition handler shares the path: {UnmappedWriteRoutes}. " +
                              "Remove options.DisableWriteSupport(), or the components declaring them.",
                writeComponents.Length,
                string.Join(", ", unmappedWriteRoutes));
        }

        static void MapGetComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionEndpointDataSource compositionDataSource,
            Dictionary<string, (Type ComponentType, IList<object> Metadata)[]> compositionOverControllerGetComponents,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpGetAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(
                    componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out var existingEndpoint)
                    && CompositionOverControllersOptions.CanComposeOver(existingEndpoint))
                {
                    logger?.LogDebug("{ComponentCount} composition component(s) registered to compose over the controller at GET {Template}.",
                        componentsGroup.Count(), componentsGroup.Key);
                    compositionOverControllerGetComponents[componentsGroup.Key] = ExtractComponentsWithMetadata(componentsGroup);
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Get}), defaultCasing, useOutputFormatters, logger, applicationServices);
                    compositionDataSource.AddEndpointBuilder(builder);
                }
            }
        }

        static void MapPostComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionEndpointDataSource compositionDataSource,
            Dictionary<string, (Type ComponentType, IList<object> Metadata)[]> compositionOverControllerPostComponents,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPostAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(
                    componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out var existingEndpoint)
                    && CompositionOverControllersOptions.CanComposeOver(existingEndpoint))
                {
                    logger?.LogDebug("{ComponentCount} composition component(s) registered to compose over the controller at POST {Template}.",
                        componentsGroup.Count(), componentsGroup.Key);
                    compositionOverControllerPostComponents[componentsGroup.Key] = ExtractComponentsWithMetadata(componentsGroup);
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Post}), defaultCasing, useOutputFormatters, logger, applicationServices);
                    compositionDataSource.AddEndpointBuilder(builder);
                }
            }
        }

        static void MapPatchComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            CompositionEndpointDataSource compositionDataSource,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPatchAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Patch}), defaultCasing, useOutputFormatters, logger, applicationServices);

                compositionDataSource.AddEndpointBuilder(builder);
            }
        }

        static void MapPutComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            CompositionEndpointDataSource compositionDataSource,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPutAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Put}), defaultCasing, useOutputFormatters, logger, applicationServices);

                compositionDataSource.AddEndpointBuilder(builder);
            }
        }

        static void MapDeleteComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            CompositionEndpointDataSource compositionDataSource,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpDeleteAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, logger);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Delete}), defaultCasing, useOutputFormatters, logger, applicationServices);

                compositionDataSource.AddEndpointBuilder(builder);
            }
        }

        static bool ThereIsAlreadyAnEndpointForTheSameTemplate(
            IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)> componentsGroup,
            ICollection<EndpointDataSource> dataSources,
            bool useCaseInsensitiveRouteMatching, out Endpoint endpoint)
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
                        var rawTemplate = useCaseInsensitiveRouteMatching
                            ? e.RoutePattern.RawText.ToLowerInvariant()
                            : e.RoutePattern.RawText;
                        return rawTemplate == componentsGroup.Key;
                    });

                return endpoint != null;
            }

            endpoint = null;
            return false;
        }

        static (Type ComponentType, IList<object> Metadata)[] ExtractComponentsWithMetadata(
            IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)> componentsGroup)
        {
            return componentsGroup
                .Select(c => (c.ComponentType, (IList<object>)c.Metadata.ToArray()))
                .ToArray();
        }

        static CompositionEndpointBuilder CreateCompositionEndpointBuilder(
            IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)> componentsGroup,
            HttpMethodMetadata methodMetadata,
            ResponseCasing? defaultCasing,
            bool useOutputFormatters,
            ILogger logger,
            IServiceProvider applicationServices)
        {
            var builder = new CompositionEndpointBuilder(
                RoutePatternFactory.Parse(componentsGroup.Key),
                componentsGroup.Select(component => component.ComponentType).ToArray(),
                0,
                defaultCasing,
                useOutputFormatters)
            {
                DisplayName = componentsGroup.Key,
                ApplicationServices = applicationServices,
            };
            builder.Metadata.Add(methodMetadata);

            builder.Metadata.Add(new CompositionEndpointComponents(
                componentsGroup.Select(component => CompositionComponent.From(component.ComponentType)).ToArray()));

            // builds a list of metadata for each component in this endpoint
            foreach (var component in componentsGroup)
            {
                builder.ComponentsMetadata.Add((component.ComponentType, component.Metadata.ToArray()));
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

        static IEnumerable<IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)>>
            SelectComponentsGroupedByTemplate<TAttribute>(
                CompositionMetadataRegistry compositionMetadataRegistry, bool useCaseInsensitiveRouteMatching, ILogger logger)
            where TAttribute : HttpMethodAttribute
        {
            var getComponentsGroupedByTemplate = compositionMetadataRegistry.Components
                .SelectMany<KeyValuePair<Type, IReadOnlyList<object>>, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)>(component =>
                {
                    var componentsAndTemplate = component.Value.OfType<TAttribute>()
                        .Select(a =>
                        {
                            var template = RouteTemplates.Normalize(a.Template);
                            return (component.Key, component.Value, useCaseInsensitiveRouteMatching
                                ? template?.ToLowerInvariant()
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

        // A route whose components are only event subscribers or view model factories has nothing
        // to compose: mapping it would make its requests run the composition only to return a 404,
        // and, when composing over a controller, replace the controller response with a 404.
        static bool HasCompositionHandlers<TAttribute>(
            IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)> componentsGroup,
            ILogger logger)
            where TAttribute : HttpMethodAttribute
        {
            if (componentsGroup.Any(component => typeof(ICompositionRequestsHandler).IsAssignableFrom(component.ComponentType)))
            {
                return true;
            }

            var httpMethods = componentsGroup.First().Metadata.OfType<TAttribute>().First().HttpMethods;
            logger?.LogWarning("No composition handler is registered for {HttpMethods} /{Template}, so the route isn't mapped, " +
                               "and these components never run for it: {Components}. Check the template for typos, or add a composition handler for the route.",
                string.Join(", ", httpMethods),
                componentsGroup.Key,
                string.Join(", ", componentsGroup.Select(component => component.ComponentType.Name).Distinct()));

            return false;
        }
    }
}