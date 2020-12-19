#if NETCOREAPP3_1 || NET5_0

using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestExtensions
    {
        internal static readonly string ComposedResponseModelKey = "composed-response-model";

        public static dynamic GetComposedResponseModel(this HttpRequest request)
        {
            return request.HttpContext.Items[ComposedResponseModelKey];
        }

        internal static void SetModel(this HttpRequest request, dynamic viewModel)
        {
            request.HttpContext.Items.Add(HttpRequestExtensions.ComposedResponseModelKey, viewModel);
        }
        
        public static ICompositionContext GetCompositionContext(this HttpRequest request)
        {
            return (ICompositionContext)request.HttpContext.Items[ComposedResponseModelKey];
        }
    }
}

#endif