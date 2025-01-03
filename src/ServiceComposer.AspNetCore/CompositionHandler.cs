using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace ServiceComposer.AspNetCore
{
    public static partial class CompositionHandler
    {
        internal static async Task<object> HandleComposableRequest(HttpContext context, CompositionContext compositionContext, Type[] componentsTypes)
        {
            var request = context.Request;

            var factoryType = componentsTypes.SingleOrDefault(t => typeof(IEndpointScopedViewModelFactory).IsAssignableFrom(t)) ?? typeof(IViewModelFactory);
            var viewModelFactory = (IViewModelFactory)context.RequestServices.GetService(factoryType); 
            var viewModel = viewModelFactory != null ? viewModelFactory.CreateViewModel(context, compositionContext) : new ExpandoObject();

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
                    .Select(handler =>
                    {
                        // TODO: apply composition filter here not before
                        // invoking the whole composition process
                        return handler.Handle(request);
                    })
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