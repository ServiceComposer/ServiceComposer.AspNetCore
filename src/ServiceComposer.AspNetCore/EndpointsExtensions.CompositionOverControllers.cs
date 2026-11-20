using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    public static partial class EndpointsExtensions
    {
        /// <summary>
        /// Runs composition over controllers for the given controller endpoints as an endpoint
        /// filter, instead of as an MVC result filter. Endpoint filters added to the same
        /// endpoints before calling this method wrap both the controller action and the
        /// composition handlers, and see the action result set by composition handlers.
        /// </summary>
        /// <remarks>
        /// Composition must be the innermost endpoint filter: call this method after adding
        /// any other endpoint filter. Requires composition over controllers to be enabled
        /// with <see cref="ViewModelCompositionOptions.EnableCompositionOverControllers"/>.
        /// </remarks>
        public static ControllerActionEndpointConventionBuilder WithCompositionOverControllers(this ControllerActionEndpointConventionBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.AddEndpointFilterFactory((factoryContext, next) =>
            {
                var options = factoryContext.ApplicationServices.GetRequiredService<ViewModelCompositionOptions>();
                if (!options.CompositionOverControllersOptions.IsEnabled)
                {
                    throw new InvalidOperationException(
                        $"Composition over controllers is not enabled. Call {nameof(ViewModelCompositionOptions.EnableCompositionOverControllers)} " +
                        $"when configuring ServiceComposer with {nameof(ServiceCollectionExtensions.AddViewModelComposition)}.");
                }

                var filter = new CompositionOverControllersEndpointFilter(
                    factoryContext.ApplicationServices.GetRequiredService<ILogger<CompositionOverControllersEndpointFilter>>());

                return invocationContext => filter.InvokeAsync(invocationContext, next);
            });

            return builder;
        }
    }
}
