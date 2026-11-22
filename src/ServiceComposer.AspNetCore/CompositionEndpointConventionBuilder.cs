using System;
using Microsoft.AspNetCore.Builder;

namespace ServiceComposer.AspNetCore
{
    /// <summary>
    /// Builds conventions for the composition endpoints mapped by <c>MapCompositionHandlers()</c>.
    /// Libraries can add extension methods to it that apply only to composition endpoints.
    /// </summary>
    public sealed class CompositionEndpointConventionBuilder : IEndpointConventionBuilder
    {
        readonly IEndpointConventionBuilder inner;

        internal CompositionEndpointConventionBuilder(IEndpointConventionBuilder inner)
        {
            this.inner = inner;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention) => inner.Add(convention);

        /// <inheritdoc />
        public void Finally(Action<EndpointBuilder> finallyConvention) => inner.Finally(finallyConvention);
    }
}
