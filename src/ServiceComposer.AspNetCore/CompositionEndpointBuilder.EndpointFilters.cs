using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComposer.AspNetCore;

partial class CompositionEndpointBuilder
{
    static readonly object buildAndCacheEndpointFilterDelegatePipelineSyncLock = new ();
    EndpointFilterDelegate BuildAndCacheEndpointFilterDelegatePipeline(IServiceProvider serviceProvider)
    {
        lock (buildAndCacheEndpointFilterDelegatePipelineSyncLock)
        {
            EndpointFilterDelegate filteredInvocation = async context =>
            {
                if (context.HttpContext.Response.StatusCode < 400)
                {
                    // The composer is per-request: read it back from HttpContext.Items
                    // here so the cached pipeline does not close over a stale composer.
                    var composer = (RequestDelegate)context.HttpContext.Items[CurrentComposerKey]!;
                    await composer(context.HttpContext);
                    var viewModel = context.HttpContext.Request.GetComposedResponseModel();

                    var containsActionResult =
                        context.HttpContext.Items.ContainsKey(HttpRequestExtensions.ComposedActionResultKey);
                    switch (useOutputFormatters)
                    {
                        case false when containsActionResult:
                            throw new NotSupportedException(
                                $"Setting an action result requires output formatters support. " +
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

            var factoryContext = new EndpointFilterFactoryContext
            {
                MethodInfo = filteredInvocation.Method,
                ApplicationServices = serviceProvider
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