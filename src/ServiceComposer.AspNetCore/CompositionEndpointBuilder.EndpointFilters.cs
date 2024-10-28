using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
    static readonly object buildAndCacheEndpointFilterDelegatePipelineSyncLock = new ();
    EndpointFilterDelegate BuildAndCacheEndpointFilterDelegatePipeline(RequestDelegate composer, IServiceProvider serviceProvider)
    {
        lock (buildAndCacheEndpointFilterDelegatePipelineSyncLock)
        {
            var compositionFiltersPipeline = BuildCompositionRequestFilterDelegatePipeline(composer, serviceProvider);
            
            var factoryContext = new EndpointFilterFactoryContext
            {
                MethodInfo = compositionFiltersPipeline.Method,
                ApplicationServices = serviceProvider
            };

            EndpointFilterDelegate filteredInvocation = async context =>
            {
                if (context.HttpContext.Response.StatusCode < 400)
                {
                    var viewModel = await compositionFiltersPipeline(new CompositionRequestFilterContext(context.HttpContext));

                    var containsActionResult =
                        context.HttpContext.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                    switch (useOutputFormatters)
                    {
                        case false when containsActionResult:
                            throw new NotSupportedException(
                                $"Setting an action result requires output formatters supports. " +
                                $"Enable output formatters by setting to true the {nameof(ResponseSerializationOptions.UseOutputFormatters)} " +
                                $"configuration property in the {nameof(ResponseSerializationOptions)} options.");
                        case true when containsActionResult:
                            return context.HttpContext.Items[HttpRequestExtensions.ComposedActionResultKey] as
                                IActionResult;
                    }

                    return viewModel;
                }

                return EmptyHttpResult.Instance;
            };

            //var terminatorFilterDelegate = filteredInvocation;
            for (var i = FilterFactories.Count - 1; i >= 0; i--)
            {
                var currentFilterFactory = FilterFactories[i];
                filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
            }

            // cachedPipeline = ReferenceEquals(terminatorFilterDelegate, filteredInvocation)
            //     ? terminatorFilterDelegate // The filter factories have run without modifications, skip running the pipeline.
            //     : filteredInvocation;
            cachedPipeline = filteredInvocation;

            return cachedPipeline;
        }
    }
}