using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    public static partial class EndpointsExtensions
    {
        /// <summary>
        /// Enables composition over controllers for the given controller endpoints, running
        /// composition handlers matching their routes as an endpoint filter, right after the
        /// controller action. Endpoint filters added to the same endpoints before calling this
        /// method wrap both the controller action and the composition handlers, and see the
        /// action result set by composition handlers.
        /// </summary>
        /// <param name="builder">The controller endpoints.</param>
        /// <param name="useCaseInsensitiveRouteMatching">Whether composition handlers routes
        /// match controller routes ignoring case.</param>
        /// <remarks>
        /// Composition must be the innermost endpoint filter: call this method after adding
        /// any other endpoint filter. Map the controllers before calling
        /// <see cref="MapCompositionHandlers"/>.
        /// The metadata of the components composing over a controller endpoint, e.g. their
        /// authorization attributes, is added to the controller endpoint, along with a
        /// <see cref="CompositionEndpointComponents"/> item. Conventions added after calling
        /// this method see it.
        /// </remarks>
        public static ControllerActionEndpointConventionBuilder WithCompositionOverControllers(
            this ControllerActionEndpointConventionBuilder builder, bool useCaseInsensitiveRouteMatching = true)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.WithMetadata(new CompositionOverControllersMetadata(useCaseInsensitiveRouteMatching));
            builder.Add(endpointBuilder => AddComposedComponentsMetadata(endpointBuilder, useCaseInsensitiveRouteMatching));
            builder.AddEndpointFilterFactory((factoryContext, next) =>
            {
                var filter = new CompositionOverControllersEndpointFilter(
                    factoryContext.ApplicationServices.GetRequiredService<ILogger<CompositionOverControllersEndpointFilter>>());

                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });

            return builder;
        }

        // Requests composing over a controller run on the controller endpoint, so the metadata of
        // the components composing over it, e.g. their authorization requirements, must be on it
        // to be enforced. As on composition endpoints, IAllowAnonymous is left out if anything on
        // the route requires authorization, including the controller endpoint's own metadata.
        // The components' metadata goes first, so the controller's wins where ASP.NET Core uses
        // the last item of a type.
        static void AddComposedComponentsMetadata(EndpointBuilder endpointBuilder, bool useCaseInsensitiveRouteMatching)
        {
            if (endpointBuilder is not RouteEndpointBuilder routeEndpointBuilder)
            {
                return;
            }

            var compositionMetadataRegistry = endpointBuilder.ApplicationServices.GetService<CompositionMetadataRegistry>();
            if (compositionMetadataRegistry == null)
            {
                return;
            }

            var routeHttpMethods = endpointBuilder.Metadata.OfType<IHttpMethodMetadata>().LastOrDefault()?.HttpMethods;
            bool HandlesMethod(string httpMethod) =>
                routeHttpMethods == null || routeHttpMethods.Count == 0 || routeHttpMethods.Contains(httpMethod, StringComparer.OrdinalIgnoreCase);

            // Only GET and POST requests compose over controllers
            var componentsGroups = Enumerable.Empty<IGrouping<string, (Type ComponentType, IReadOnlyList<object> Metadata, string Template)>>();
            if (HandlesMethod(HttpMethods.Get))
            {
                componentsGroups = componentsGroups.Concat(
                    SelectComponentsGroupedByTemplate<HttpGetAttribute>(compositionMetadataRegistry, useCaseInsensitiveRouteMatching, logger: null));
            }
            if (HandlesMethod(HttpMethods.Post))
            {
                componentsGroups = componentsGroups.Concat(
                    SelectComponentsGroupedByTemplate<HttpPostAttribute>(compositionMetadataRegistry, useCaseInsensitiveRouteMatching, logger: null));
            }

            var components = componentsGroups
                .Where(componentsGroup => TemplateMatches(routeEndpointBuilder.RoutePattern, componentsGroup.Key, useCaseInsensitiveRouteMatching))
                .SelectMany(componentsGroup => componentsGroup)
                .DistinctBy(component => component.ComponentType)
                .ToArray();
            if (components.Length == 0)
            {
                return;
            }

            var componentsRequiringAuthorization = components
                .Where(component => component.Metadata.OfType<IAuthorizeData>().Any() && !component.Metadata.OfType<IAllowAnonymous>().Any())
                .Select(component => component.ComponentType.Name)
                .ToList();
            var endpointRequiresAuthorization = endpointBuilder.Metadata.OfType<IAuthorizeData>().Any()
                                                && !endpointBuilder.Metadata.OfType<IAllowAnonymous>().Any();
            var anyRequiresAuthorization = endpointRequiresAuthorization || componentsRequiringAuthorization.Count > 0;
            if (anyRequiresAuthorization)
            {
                var allowingAnonymous = components
                    .Where(component => component.Metadata.OfType<IAllowAnonymous>().Any())
                    .Select(component => component.ComponentType.Name)
                    .ToList();
                if (endpointBuilder.Metadata.OfType<IAllowAnonymous>().Any())
                {
                    allowingAnonymous.Add(endpointBuilder.DisplayName);
                    foreach (var allowAnonymous in endpointBuilder.Metadata.OfType<IAllowAnonymous>().ToArray())
                    {
                        endpointBuilder.Metadata.Remove(allowAnonymous);
                    }
                }

                if (endpointRequiresAuthorization)
                {
                    componentsRequiringAuthorization.Insert(0, endpointBuilder.DisplayName);
                }

                if (allowingAnonymous.Count > 0)
                {
                    endpointBuilder.ApplicationServices.GetService<ILoggerFactory>()?
                        .CreateLogger(typeof(EndpointsExtensions))
                        .LogWarning("[AllowAnonymous] on {ComponentsAllowingAnonymous} is ignored for {HttpMethods} {Template}, " +
                                    "since authorization is required by {ComponentsRequiringAuthorization}: the most restrictive requirements apply to the route.",
                            string.Join(", ", allowingAnonymous),
                            string.Join(", ", routeHttpMethods ?? [HttpMethods.Get, HttpMethods.Post]),
                            RouteTemplates.Normalize(routeEndpointBuilder.RoutePattern.RawText),
                            string.Join(", ", componentsRequiringAuthorization));
                }
            }

            var componentsMetadata = components
                .SelectMany(component => component.Metadata)
                .Where(metadata => !(anyRequiresAuthorization && metadata is IAllowAnonymous))
                .Append(new CompositionEndpointComponents(
                    components.Select(component => CompositionComponent.From(component.ComponentType)).ToArray()))
                .ToArray();
            for (var i = 0; i < componentsMetadata.Length; i++)
            {
                endpointBuilder.Metadata.Insert(i, componentsMetadata[i]);
            }
        }

        /// <summary>
        /// Composition over controllers is enabled for the endpoints using WithCompositionOverControllers().
        /// </summary>
        static CompositionOverControllersOptions ResolveCompositionOverControllersOptions(
            ICollection<EndpointDataSource> dataSources)
        {
            var optedInEndpointsSettings = dataSources
                .Where(dataSource => dataSource is not CompositionEndpointDataSource)
                .SelectMany(dataSource => dataSource.Endpoints)
                .Select(endpoint => endpoint.Metadata.GetMetadata<CompositionOverControllersMetadata>())
                .Where(metadata => metadata != null)
                .Select(metadata => metadata.UseCaseInsensitiveRouteMatching)
                .Distinct()
                .ToList();

            if (optedInEndpointsSettings.Count > 1)
            {
                throw new InvalidOperationException(
                    "Composition over controllers is configured with different useCaseInsensitiveRouteMatching values. " +
                    "Use the same value for all WithCompositionOverControllers() calls.");
            }

            if (optedInEndpointsSettings.Count == 0)
            {
                return new CompositionOverControllersOptions();
            }

            return new CompositionOverControllersOptions
            {
                IsEnabled = true,
                UseCaseInsensitiveRouteMatching = optedInEndpointsSettings[0]
            };
        }
    }
}
