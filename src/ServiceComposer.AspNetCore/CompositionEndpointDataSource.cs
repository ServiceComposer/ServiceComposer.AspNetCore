using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace ServiceComposer.AspNetCore
{
    class CompositionEndpointDataSource : EndpointDataSource
    {
        readonly List<CompositionEndpointBuilder> _endpointBuilders = new List<CompositionEndpointBuilder>();

        public void AddEndpointBuilder(CompositionEndpointBuilder endpointBuilder)
        {
            _endpointBuilders.Add(endpointBuilder);
        }

        public override IChangeToken GetChangeToken()
        {
            return NullChangeToken.Singleton;
        }

        public override IReadOnlyList<Endpoint> Endpoints => _endpointBuilders
            .OrderBy(builder=>builder.Order)
            .Select(builder => builder.Build()).ToArray();
    }
}