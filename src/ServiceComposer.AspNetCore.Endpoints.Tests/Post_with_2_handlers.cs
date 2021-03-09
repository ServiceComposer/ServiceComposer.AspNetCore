using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class Post_with_2_handlers
    {
        class TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<IntegerRequest>();

                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.Body.ANumber;
            }
        }


        class TestStringHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<StringRequest>();

                var vm = request.GetComposedResponseModel();
                vm.AString = model.Body.AString;
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var expectedString = "this is a string value";
            var expectedNumber = 32;

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Post_with_2_handlers>
            (
                configureServices: services =>
                {
                    //TODO: this need to be moved into something like options.EnableModelBinding();
                    services.AddSingleton<Binder>();

                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStringHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.EnableWriteSupport();
                    });
                    services.AddRouting();

                    //TODO: this is a requirement when using model binding. How to enforce it?
                    services.AddControllers();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            var json = JsonConvert.SerializeObject(new ClientRequestModel
            {
                AString = expectedString,
                ANumber = expectedNumber
            });
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            var response = await client.PostAsync("/sample/1", stringContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);
            Assert.Equal(expectedString, responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(expectedNumber, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }

    class ClientRequestModel
    {
        public string AString { get; set; }
        public int ANumber { get; set; }
    }

    class IntegerRequest
    {
        [FromBody] public IntegerModel Body { get; set; }
    }

    class StringRequest
    {
        [FromBody] public StringModel Body { get; set; }
    }

    class IntegerModel
    {
        public int ANumber { get; set; }
    }

    class StringModel
    {
        public string AString { get; set; }
    }

    static class BinderRequestExtension
    {
        public static Task<T> Bind<T>(this HttpRequest request) where T : new()
        {
            var context = request.HttpContext;
            Binder binder;
            try
            {
                binder = context.RequestServices.GetRequiredService<Binder>();
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

    class Binder
    {
        IModelBinderFactory modelBinderFactory;
        IModelMetadataProvider modelMetadataProvider;
        IOptions<MvcOptions> mvcOptions;

        public Binder(IModelBinderFactory modelBinderFactory, IModelMetadataProvider modelMetadataProvider, IOptions<MvcOptions> mvcOptions)
        {
            this.modelBinderFactory = modelBinderFactory;
            this.modelMetadataProvider = modelMetadataProvider;
            this.mvcOptions = mvcOptions;
        }

        public async Task<T> Bind<T>(HttpRequest request) where T : new()
        {
            //always rewind the stream; otherwise,
            //if multiple handlers concurrently bind
            //different models only the first one succeeds
            request.Body.Position = 0;

            var modelType = typeof(T);
            var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
            var actionContext = new ActionContext(
                request.HttpContext,
                request.HttpContext.GetRouteData(),
                new ActionDescriptor(),
                new ModelStateDictionary());
            var valueProvider =
                await CompositeValueProvider.CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories);

#if NET5_0
            if (modelMetadata.BoundConstructor != null)
            {
                throw new NotSupportedException("Record type not supported");
            }
#endif

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                modelMetadata,
                bindingInfo: null,
                modelName: "");

            modelBindingContext.Model = new T();
            modelBindingContext.PropertyFilter = _ => true; // All props

            var factoryContext = new ModelBinderFactoryContext()
            {
                Metadata = modelMetadata,
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = modelMetadata.BinderModelName,
                    BinderType = modelMetadata.BinderType,
                    BindingSource = modelMetadata.BindingSource,
                    PropertyFilterProvider = modelMetadata.PropertyFilterProvider,
                },
                CacheToken = modelMetadata,
            };

            await modelBinderFactory
                .CreateBinder(factoryContext)
                .BindModelAsync(modelBindingContext);

            return (T) modelBindingContext.Result.Model;
        }
    }
}