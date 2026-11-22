#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

static class ComponentsModelBinder
{
    // Returns the binding errors of all the components too, merged
    internal static async Task<(IDictionary<Type, IList<ModelBindingArgument>> Arguments, ModelStateDictionary ModelState)> BindAll(
        IEnumerable<(Type ComponentType, IList<object> Metadata)> componentsMetadata,
        HttpContext context)
    {
        var result = new Dictionary<Type, IList<ModelBindingArgument>>();
        var modelState = new ModelStateDictionary();
        foreach (var componentMetadata in componentsMetadata)
        {
            // Arguments are looked up by name and binding source, so each pair is bound once. The
            // method's attributes come after the class' ones, so the last declared, the method's, wins.
            var modelAttributes = componentMetadata.Metadata
                .OfType<BindModelAttribute>()
                .GroupBy(attribute => (ModelName: attribute.ModelName ?? "", attribute.BindingSource))
                .Select(group => group.Last());

            // A component can have more than one Http* attribute
            // If that's the case we don't want to have more than one
            // arguments lists. Instead, we're reusing an existing one.
            if (!result.TryGetValue(componentMetadata.ComponentType, out var arguments))
            {
                arguments = new List<ModelBindingArgument>();
                result.Add(componentMetadata.ComponentType, arguments);
            }

            foreach (var modelAttribute in modelAttributes)
            {
                // Already bound for the same component, composed into this endpoint more than once
                if (arguments.Any(argument => (argument.Name ?? "") == (modelAttribute.ModelName ?? "") && argument.BindingSource == modelAttribute.BindingSource))
                {
                    continue;
                }

                if (modelAttribute.BindingSource == BindingSource.Services)
                {
                    var service = context.RequestServices.GetRequiredService(modelAttribute.Type);
                    arguments.Add(new ModelBindingArgument(modelAttribute.ModelName!, service, modelAttribute.BindingSource));
                    continue;
                }

                // TODO: shall we cache the instance? We cannot access it earlier otherwise we need model binding support for every request even if it's not needed by user code
                var binder = context.RequestServices.GetRequiredService<RequestModelBinder>();
                var bindingResult = await binder.TryBind(
                    modelAttribute.Type,
                    context.Request,
                    modelAttribute.ModelName ?? "",
                    modelAttribute.BindingSource);
                ModelBindingProblem.AddErrors(modelState, bindingResult.ModelState);
                arguments.Add(new ModelBindingArgument(modelAttribute.ModelName!, bindingResult.Model, modelAttribute.BindingSource));
            }
        }

        return (result, modelState);
    }
}
