using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        /// </remarks>
        public static ControllerActionEndpointConventionBuilder WithCompositionOverControllers(
            this ControllerActionEndpointConventionBuilder builder, bool useCaseInsensitiveRouteMatching = true)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.WithMetadata(new CompositionOverControllersMetadata(useCaseInsensitiveRouteMatching));
            builder.AddEndpointFilterFactory((factoryContext, next) =>
            {
                var filter = new CompositionOverControllersEndpointFilter(
                    factoryContext.ApplicationServices.GetRequiredService<ILogger<CompositionOverControllersEndpointFilter>>());

                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });

            return builder;
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
