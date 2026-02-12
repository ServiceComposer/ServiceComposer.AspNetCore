#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
    Task<IDictionary<Type, IList<ModelBindingArgument>>> GetAllComponentsArguments(HttpContext context)
    {
        return ComponentsModelBinder.BindAll(ComponentsMetadata, context);
    }
}
