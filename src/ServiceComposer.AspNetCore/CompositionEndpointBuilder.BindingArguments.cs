#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
    Task<(IDictionary<Type, IList<ModelBindingArgument>> Arguments, ModelStateDictionary ModelState)> GetAllComponentsArguments(HttpContext context)
    {
        return ComponentsModelBinder.BindAll(ComponentsMetadata, context);
    }
}
