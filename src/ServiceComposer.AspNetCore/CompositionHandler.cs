using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceComposer.AspNetCore
{
    public static partial class CompositionHandler
    {
        internal static async Task<object> HandleComposableRequest(HttpContext context, Type[] componentsTypes)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var routeData = context.GetRouteData();

            if(!request.Headers.TryGetValue(ComposedRequestIdHeader.Key, out var requestId))
            {
                requestId = Guid.NewGuid().ToString();
            }

            context.Response.Headers.Append(ComposedRequestIdHeader.Key, requestId);
            var compositionContext = new CompositionContext(requestId, routeData, request);

            object viewModel;
            var factoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t)) ?? typeof(IViewModelFactory);
            var viewModelFactory = (IViewModelFactory)context.RequestServices.GetService(factoryType);
            if (viewModelFactory != null)
            {
                viewModel = viewModelFactory.CreateViewModel(context, compositionContext);
            }
            else
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<DynamicViewModel>>();
                viewModel = new DynamicViewModel(logger);
            }

            try
            {
                request.SetViewModel(viewModel);
                request.SetCompositionContext(compositionContext);

                await Task.WhenAll(context.RequestServices.GetServices<IViewModelPreviewHandler>()
                    .Select(visitor => visitor.Preview(request))
                    .ToList());

                var handlers = componentsTypes.Select(type => context.RequestServices.GetRequiredService(type)).ToArray();
                //TODO: if handlers == none we could shortcut to 404 here

                foreach (var subscriber in handlers.OfType<ICompositionEventsSubscriber>())
                {
                    subscriber.Subscribe(compositionContext);
                }

                //TODO: if handlers == none we could shortcut again to 404 here
                var pending = handlers.OfType<ICompositionRequestsHandler>()
                    .Select(handler => handler.Handle(request))
                    .ToList();

                if (pending.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return null;
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        //TODO: refactor to Task.WhenAll
                        var errorHandlers = handlers.OfType<ICompositionErrorsHandler>();
                        foreach (var handler in errorHandlers)
                        {
                            await handler.OnRequestError(request, ex);
                        }

                        throw;
                    }
                }

                return viewModel;
            }
            finally
            {
                compositionContext.CleanupSubscribers();
            }
        }
    }
}