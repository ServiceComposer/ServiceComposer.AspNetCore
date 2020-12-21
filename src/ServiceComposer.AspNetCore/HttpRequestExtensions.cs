#if NETCOREAPP3_1 || NET5_0

using System;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestExtensions
    {
        internal static readonly string ComposedResponseModelKey = "composed-response-model";
        internal static readonly string CompositionContextKey = "composition-context";

        public static dynamic GetComposedResponseModel(this HttpRequest request)
        {
            return request.HttpContext.Items[ComposedResponseModelKey];
        }

#if NETCOREAPP3_1 || NET5_0
        public static T GetComposedResponseModel<T>(this HttpRequest request) where T : class
        {
            var vm = request.HttpContext.Items[ComposedResponseModelKey];
            if (vm is T t)
            {
                return t;
            }

            var message = $"Cannot convert view model to {typeof(T).Name}. " +
                          $"Make sure a custom view model factory is registered " +
                          $"and that the created view model is of type {typeof(T).Name}.";
            throw new InvalidCastException(message);
        }
#endif

        internal static void SetViewModel(this HttpRequest request, dynamic viewModel)
        {
            request.HttpContext.Items.Add(HttpRequestExtensions.ComposedResponseModelKey, viewModel);
        }

        internal static void SetCompositionContext(this HttpRequest request, ICompositionContext compositionContext)
        {
            request.HttpContext.Items.Add(HttpRequestExtensions.CompositionContextKey, compositionContext);
        }

        public static ICompositionContext GetCompositionContext(this HttpRequest request)
        {
            return (ICompositionContext)request.HttpContext.Items[CompositionContextKey];
        }
    }
}

#endif