using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ServiceComposer.AspNetCore
{
    public class CompositionHandler
    {
        public static async Task<(dynamic ViewModel, int StatusCode)> HandleRequest(string requestId,
            HttpContext context)
        {
            var routeData = context.GetRouteData();
            var request = context.Request;
            var viewModel = new DynamicViewModel(requestId, routeData, context.Request);

            try
            {
                var interceptors = context.RequestServices.GetServices<IInterceptRoutes>()
                    .Where(a => a.Matches(routeData, request.Method, request))
                    .ToArray();

                foreach (var subscriber in interceptors.OfType<ISubscribeToCompositionEvents>())
                {
                    subscriber.Subscribe(viewModel);
                }

                var pending = new List<Task>();

                foreach (var handler in interceptors.OfType<IHandleRequests>())
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
                        var errorHandlers = interceptors.OfType<IHandleRequestsErrors>();
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
                viewModel.CleanupSubscribers();
            }
        }

#if NETCOREAPP3_1 || NET5_0
        internal static async Task<dynamic> HandleComposableRequest(HttpContext context, Type[] handlerTypes)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var routeData = context.GetRouteData();

            var requestId = request.Headers.GetComposedRequestIdHeaderOr(() =>
            {
                var id = Guid.NewGuid().ToString();
                context.Request.Headers.AddComposedRequestIdHeader(id);
                return id;
            });

            context.Response.Headers.AddComposedRequestIdHeader(requestId);

            var viewModel = new DynamicViewModel(requestId, routeData, request);

            await Task.WhenAll(context.RequestServices.GetServices<IViewModelPreviewHandler>()
                .Select(visitor => visitor.Preview(request, viewModel))
                .ToList());

            try
            {
                request.SetModel(viewModel);

                var handlers = handlerTypes.Select(type => context.RequestServices.GetRequiredService(type)).ToArray();

                foreach (var subscriber in handlers.OfType<ICompositionEventsSubscriber>())
                {
                    subscriber.Subscribe(viewModel);
                }

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
                viewModel.CleanupSubscribers();
            }
        }
#endif
    }
}