#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
    async Task<IList<(Type ComponentType, IList<object?> Arguments)>> GetAllComponentsArguments(HttpContext context)
    {
        var result = new List<(Type ComponentType, IList<object?> Arguments)>();
        foreach (var componentMetadata in ComponentsMetadata)
        {
            var modelAttributes = componentMetadata.Metadata.OfType<ModelAttribute>();
            var arguments = new List<object?>();
            foreach (var modelAttribute in modelAttributes)
            {
                // TODO: shall we cache the instance? We cannot access it earlier otherwise we need model binding support for every request even if it's not needed by user code
                var binder = context.RequestServices.GetRequiredService<RequestModelBinder>();
                var bindingResult = await binder.TryBind(modelAttribute.Type, context.Request);
                //TODO: throw if binding failed
                arguments.Add(bindingResult.Model);
            }
            
            result.Add((componentMetadata.ComponentType, arguments));
        }

        return result;
    }
}