using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public static class HttpRequestModelBinderExtension
    {
        /// <summary>
        /// Binds a model from the request. On binding errors, throws a <see cref="BadHttpRequestException"/>
        /// with status code <c>400</c>, which composition turns into a <c>400 Bad Request</c> with a validation
        /// problem details body, unless <see cref="ModelBindingOptions.FailOnBindingErrors"/> is <c>false</c>.
        /// Use <see cref="TryBind{T}"/> to inspect the binding errors instead.
        /// </summary>
        public static async Task<T> Bind<T>(this HttpRequest request) where T : new()
        {
            var binderResult = await TryBind<T>(request);
            if (binderResult.ModelState.ErrorCount > 0 && ModelBindingProblem.FailOnBindingErrors(request.HttpContext))
            {
                throw ModelBindingProblem.CreateException(typeof(T), binderResult.ModelState);
            }

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