using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestModelBinderExtension
    {
        public static async Task<T> Bind<T>(this HttpRequest request) where T : new()
        {
            var binderResult = await TryBind<T>(request);
            return binderResult.Model;
        }

        public static Task<(T Model, bool IsModelSet, ModelStateDictionary ModelState)> TryBind<T>(
            this HttpRequest request) where T : new()
        {
            var context = request.HttpContext;
            var binder = context.RequestServices.GetRequiredService<RequestModelBinder>();
            return binder.TryBind<T>(request);
        }
    }
}