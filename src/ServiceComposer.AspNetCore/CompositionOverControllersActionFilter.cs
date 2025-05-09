﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersActionFilter : IAsyncResultFilter
    {
        readonly CompositionOverControllersRoutes _compositionOverControllersRoutes;
        readonly CompositionOverControllersOptions _compositionOverControllersOptions;

        public CompositionOverControllersActionFilter(CompositionOverControllersRoutes compositionOverControllersRoutes, ViewModelCompositionOptions viewModelCompositionOptions)
        {
            _compositionOverControllersRoutes = compositionOverControllersRoutes;
            _compositionOverControllersOptions = viewModelCompositionOptions.CompositionOverControllersOptions;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.HttpContext.GetEndpoint() is RouteEndpoint endpoint)
            {
                Debug.Assert(endpoint.RoutePattern.RawText != null, "endpoint.RoutePattern.RawText != null");
                var rawTemplate = _compositionOverControllersOptions.UseCaseInsensitiveRouteMatching
                    ? endpoint.RoutePattern.RawText.ToLowerInvariant()
                    : endpoint.RoutePattern.RawText;
                var handlerTypes = _compositionOverControllersRoutes.HandlersForRoute(rawTemplate, context.HttpContext.Request.Method);

                if (handlerTypes.Any())
                {
                    // We need the body to be seekable otherwise if more than one
                    // composition handler tries to bind a model to the body
                    // it'll fail and only the first one succeeds
                    context.HttpContext.Request.EnableBuffering();
                    
                    var requestId = context.HttpContext.EnsureRequestIdIsSetup();
                    var compositionContext = new CompositionContext
                    (
                        requestId,
                        context.HttpContext.Request,
                        context.HttpContext.RequestServices.GetRequiredService<CompositionMetadataRegistry>(),
                        //arguments binding is unsupported when using composition over controllers
                        new Dictionary<Type, IList<ModelBindingArgument>>(),
                        usingCompositionOverControllers: true
                    );

                    var viewModel = await CompositionHandler.HandleComposableRequest(context.HttpContext, compositionContext, handlerTypes);
                    switch (context.Result)
                    {
                        case ViewResult viewResult when viewResult.ViewData.Model == null:
                        {
                            //MVC
                            viewResult.ViewData.Model = viewModel;

                            break;
                        }
                        case ObjectResult { Value: null } objectResult:
                        {
                            //WebAPI
                            objectResult.Value = viewModel;

                            break;
                        }
                    }
                }
            }

            await next();
        }
    }
}