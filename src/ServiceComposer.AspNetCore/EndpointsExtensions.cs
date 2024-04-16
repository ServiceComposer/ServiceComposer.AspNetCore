using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        static readonly Dictionary<string, Type[]> compositionOverControllerGetComponents = new();
        static readonly Dictionary<string, Type[]> compositionOverControllerPostComponents = new();

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
                compositionOverControllersRoutes.AddGetComponentsSource(compositionOverControllerGetComponents);
                compositionOverControllersRoutes.AddPostComponentsSource(compositionOverControllerPostComponents);
            }

            var compositionMetadataRegistry =
                endpoints.ServiceProvider.GetRequiredService<CompositionMetadataRegistry>();

            MapGetComponents(
                compositionMetadataRegistry,
                endpoints.DataSources,
                options.CompositionOverControllersOptions,
                options.ResponseSerialization.DefaultResponseCasing,
                options.ResponseSerialization.UseOutputFormatters);
            if (options.IsWriteSupportEnabled)
            {
                MapPostComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters);
                MapPutComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters);
                MapPatchComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters);
                MapDeleteComponents(
                    compositionMetadataRegistry,
                    endpoints.DataSources,
                    options.CompositionOverControllersOptions,
                    options.ResponseSerialization.DefaultResponseCasing,
                    options.ResponseSerialization.UseOutputFormatters);
            }
            
            var dataSource = endpoints.DataSources.OfType<CompositionEndpointDataSource>().FirstOrDefault();
            return dataSource;
        }

        static void MapGetComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpGetAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(
                    componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out _))
                {
                    var componentTypes = componentsGroup.Select(c => c.ComponentType).ToArray();
                    compositionOverControllerGetComponents[componentsGroup.Key] = componentTypes;
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Get}), defaultCasing, useOutputFormatters);
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        static void MapPostComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPostAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                if (compositionOverControllersOptions.IsEnabled && ThereIsAlreadyAnEndpointForTheSameTemplate(
                    componentsGroup, dataSources,
                    compositionOverControllersOptions.UseCaseInsensitiveRouteMatching, out _))
                {
                    var componentTypes = componentsGroup.Select(c => c.ComponentType).ToArray();
                    compositionOverControllerPostComponents[componentsGroup.Key] = componentTypes;
                }
                else
                {
                    var builder = CreateCompositionEndpointBuilder(componentsGroup,
                        new HttpMethodMetadata(new[] {HttpMethods.Post}), defaultCasing, useOutputFormatters);
                    AppendToDataSource(dataSources, builder);
                }
            }
        }

        static void MapPatchComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPatchAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Patch}), defaultCasing, useOutputFormatters);

                AppendToDataSource(dataSources, builder);
            }
        }

        static void MapPutComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpPutAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Put}), defaultCasing, useOutputFormatters);

                AppendToDataSource(dataSources, builder);
            }
        }

        static void MapDeleteComponents(CompositionMetadataRegistry compositionMetadataRegistry,
            ICollection<EndpointDataSource> dataSources,
            CompositionOverControllersOptions compositionOverControllersOptions, ResponseCasing defaultCasing,
            bool useOutputFormatters)
        {
            var componentsGroupedByTemplate = SelectComponentsGroupedByTemplate<HttpDeleteAttribute>(
                compositionMetadataRegistry, compositionOverControllersOptions.UseCaseInsensitiveRouteMatching);

            foreach (var componentsGroup in componentsGroupedByTemplate)
            {
                var builder = CreateCompositionEndpointBuilder(componentsGroup,
                    new HttpMethodMetadata(new[] {HttpMethods.Delete}), defaultCasing, useOutputFormatters);

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

        static bool ThereIsAlreadyAnEndpointForTheSameTemplate(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup,
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

        static CompositionEndpointBuilder CreateCompositionEndpointBuilder(
            IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)> componentsGroup,
            HttpMethodMetadata methodMetadata,
            ResponseCasing defaultCasing,
            bool useOutputFormatters)
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

            var attributes = componentsGroup.SelectMany(component => component.Method.GetCustomAttributes());
            foreach (var attribute in attributes)
            {
                builder.Metadata.Add(attribute);
            }

            return builder;
        }

        static IEnumerable<IGrouping<string, (Type ComponentType, MethodInfo Method, string Template)>>
            SelectComponentsGroupedByTemplate<TAttribute>(
                CompositionMetadataRegistry compositionMetadataRegistry, bool useCaseInsensitiveRouteMatching)
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
            else if (typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(componentType))
            {
                return componentType.GetMethod(nameof(IEndpointScopedViewModelFactory.CreateViewModel));
            }

            var message = $"Component needs to be either {nameof(ICompositionRequestsHandler)}, " +
                          $"{nameof(ICompositionEventsSubscriber)}, or {nameof(IEndpointScopedViewModelFactory)}.";
            throw new NotSupportedException(message);
        }
    }
}