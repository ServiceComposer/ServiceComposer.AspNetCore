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
            Binder binder;
            
            public TestIntegerHandler(Binder binder)
            {
                this.binder = binder;
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await binder.Bind<IntegerRequest>(request);

                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.Body.ANumber;
            }
        }


        class TestStrinHandler : ICompositionRequestsHandler
        {
            Binder binder;

            public TestStrinHandler(Binder binder)
            {
                this.binder = binder;
            }
            
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await binder.Bind<StringRequest>(request);

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
                    services.AddSingleton<Binder>();
                    
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStrinHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.EnableWriteSupport();
                    });
                    services.AddRouting();
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
            //if multiple handler concurrently bind
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