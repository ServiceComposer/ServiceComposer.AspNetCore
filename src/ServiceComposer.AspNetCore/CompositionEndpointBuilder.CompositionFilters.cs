using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
   CompositionRequestFilterDelegate BuildCompositionRequestFilterDelegatePipeline(RequestDelegate composer, IServiceProvider serviceProvider)
    {
        List<Func<CompositionRequestFilterFactoryContext, CompositionRequestFilterDelegate, CompositionRequestFilterDelegate>> compositionRequestFilterFactories = [];
        var compositionRequestFilters = Metadata
            .Where(obj => obj is ICompositionRequestFilter)
            .Cast<ICompositionRequestFilter>()
            .ToList();
        foreach (var componentType in componentsTypes)
        {
            var typeBasedCompositionFilterType = typeof(ICompositionRequestFilter<>).MakeGenericType(componentType);
            if (serviceProvider.GetService(typeBasedCompositionFilterType) is ICompositionRequestFilter typeBasedCompositionFilter)
            {
                compositionRequestFilters.Add(typeBasedCompositionFilter);
            }
        }
        
        compositionRequestFilters.ForEach(filter =>
        {
            compositionRequestFilterFactories.Add((factoryContext, next) => (context) => filter.InvokeAsync(context, next));
        });

        CompositionRequestFilterDelegate filteredInvocation = async context =>
        {
            await composer(context.HttpContext);
            var viewModel = context.HttpContext.Request.GetComposedResponseModel();

            return viewModel;
        };

        var factoryContext = new CompositionRequestFilterFactoryContext();
        
        //var terminatorFilterDelegate = filteredInvocation;
        for (var i = compositionRequestFilterFactories.Count - 1; i >= 0; i--)
        {
            var currentFilterFactory = compositionRequestFilterFactories[i];
            filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
        }
        
        // return ReferenceEquals(terminatorFilterDelegate, filteredInvocation)
        //     ? terminatorFilterDelegate // The filter factories have run without modifications, skip running the pipeline.
        //     : filteredInvocation;
        return filteredInvocation;
    }

}