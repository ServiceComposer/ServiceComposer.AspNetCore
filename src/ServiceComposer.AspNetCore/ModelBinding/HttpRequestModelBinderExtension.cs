#if NET5_0 || NETCOREAPP3_1

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestModelBinderExtension
    {
        public static Task<T> Bind<T>(this HttpRequest request) where T : new()
        {
            var context = request.HttpContext;
            var binder = context.RequestServices.GetRequiredService<RequestModelBinder>();

            return binder.Bind<T>(request);
        }
    }
}

#endif