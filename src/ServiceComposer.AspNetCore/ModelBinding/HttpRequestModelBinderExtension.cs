#if NET5_0 || NETCOREAPP3_1

using System;
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
            RequestModelBinder binder;
            try
            {
                binder = context.RequestServices.GetRequiredService<RequestModelBinder>();
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Unable to resolve one of the services required to support model binding. " +
                                                    "Make sure the application is configured to use MVC services by calling either " +
                                                    $"services.{nameof(MvcServiceCollectionExtensions.AddControllers)}(), or " +
                                                    $"services.{nameof(MvcServiceCollectionExtensions.AddControllersWithViews)}(), or " +
                                                    $"services.{nameof(MvcServiceCollectionExtensions.AddMvc)}(), or " +
                                                    $"services.{nameof(MvcServiceCollectionExtensions.AddRazorPages)}().", e);
            }

            return binder.Bind<T>(request);
        }
    }
}

#endif