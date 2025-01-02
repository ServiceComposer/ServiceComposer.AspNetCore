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
    async Task<IDictionary<Type, IList<ModelBindingArgument>>> GetAllComponentsArguments(HttpContext context)
    {
        var result = new Dictionary<Type, IList<ModelBindingArgument>>();
        foreach (var componentMetadata in ComponentsMetadata)
        {
            var modelAttributes = componentMetadata.Metadata.OfType<BindModelAttribute>();
            
            // A component can have more than one Http* attribute
            // If that's the case we don't want to have more than
            // arguments lists. Instead, we're reusing an existing one.
            if (!result.TryGetValue(componentMetadata.ComponentType, out var arguments))
            {
                arguments = new List<ModelBindingArgument>();
                result.Add(componentMetadata.ComponentType, arguments);
            }
            
            foreach (var modelAttribute in modelAttributes)
            {
                // TODO: shall we cache the instance? We cannot access it earlier otherwise we need model binding support for every request even if it's not needed by user code
                var binder = context.RequestServices.GetRequiredService<RequestModelBinder>();
                var bindingResult = await binder.TryBind(
                    modelAttribute.Type,
                    context.Request,
                    modelAttribute.ModelName ?? "",
                    modelAttribute.BindingSource);
                //TODO: throw if binding failed
                arguments.Add(new ModelBindingArgument(modelAttribute.ModelName!, bindingResult.Model, modelAttribute.BindingSource));
            }
        }

        return result;
    }
}