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
    public class CompositionHandler
    {
        public static async Task<(dynamic ViewModel, int StatusCode)> HandleRequest(string requestId,
            HttpContext context)
        {
            var routeData = context.GetRouteData();
            var request = context.Request;
            var compositionContext = new CompositionContext(requestId, routeData, request);
            var logger = context.RequestServices.GetRequiredService<ILogger<DynamicViewModel>>();
            var viewModel = new DynamicViewModel(logger, compositionContext);

            try
            {
#pragma warning disable 618
                var interceptors = context.RequestServices.GetServices<IInterceptRoutes>()
#pragma warning restore 618
                    .Where(a => a.Matches(routeData, request.Method, request))
                    .ToArray();

#pragma warning disable 618
                foreach (var subscriber in interceptors.OfType<ISubscribeToCompositionEvents>())
#pragma warning restore 618
                {
                    subscriber.Subscribe(viewModel);
                }

                var pending = new List<Task>();

#pragma warning disable 618
                foreach (var handler in interceptors.OfType<IHandleRequests>())
#pragma warning restore 618
                {
                    pending.Add
                    (
                        handler.Handle(requestId, viewModel, routeData, request)
                    );
                }

                if (pending.Count == 0)
                {
                    //we set this here to keep the implementation aligned with the .NET Core 3.x version
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    return (null, StatusCodes.Status404NotFound);
                }
                else
                {
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
#pragma warning disable 618
                        var errorHandlers = interceptors.OfType<IHandleRequestsErrors>();
#pragma warning restore 618
                        if (errorHandlers.Any())
                        {
                            foreach (var handler in errorHandlers)
                            {
                                await handler.OnRequestError(requestId, ex, viewModel, routeData, request);
                            }
                        }

                        throw;
                    }
                }

                return (viewModel, StatusCodes.Status200OK);
            }
            finally
            {
                compositionContext.CleanupSubscribers();
            }
        }

#if NETCOREAPP3_1 || NET5_0
        internal static async Task<object> HandleComposableRequest(HttpContext context, Type[] componentsTypes)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var routeData = context.GetRouteData();

#pragma warning disable 618
            var requestId = request.Headers.GetComposedRequestIdHeaderOr(() =>
#pragma warning restore 618
            {
                var id = Guid.NewGuid().ToString();
#pragma warning disable 618
                context.Request.Headers.AddComposedRequestIdHeader(id);
#pragma warning restore 618
                return id;
            });

#pragma warning disable 618
            context.Response.Headers.AddComposedRequestIdHeader(requestId);
#pragma warning restore 618

            var compositionContext = new CompositionContext(requestId, routeData, request);

            object viewModel = null;
            var factoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t)) ?? typeof(IViewModelFactory);
            var viewModelFactory = (IViewModelFactory)context.RequestServices.GetService(factoryType);
            if (viewModelFactory != null)
            {
                viewModel = viewModelFactory.CreateViewModel(context, compositionContext);
            }

            if (viewModel == null)
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<DynamicViewModel>>();
                viewModel = new DynamicViewModel(logger, compositionContext);
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
                    context.Response.StatusCode = (int) StatusCodes.Status404NotFound;
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
#endif
    }
}