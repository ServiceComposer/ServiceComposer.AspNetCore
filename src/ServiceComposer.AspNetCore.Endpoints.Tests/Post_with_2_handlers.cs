using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class Post_with_2_handlers
    {
        class TestIntegerHandler : ICompositionRequestsHandler
        {
            private readonly IModelBinderFactory modelBinderFactory;
            IModelMetadataProvider modelMetadataProvider;
            private IOptions<MvcOptions> mvcOptions;

            public TestIntegerHandler(IModelBinderFactory modelBinderFactory,
                IModelMetadataProvider modelMetadataProvider, IOptions<MvcOptions> mvcOptions)
            {
                this.modelBinderFactory = modelBinderFactory;
                this.modelMetadataProvider = modelMetadataProvider;
                this.mvcOptions = mvcOptions;
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await Bind<RequestWrapper>(request.HttpContext);

                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.Body.ANumber;
            }

            async Task<T> Bind<T>(HttpContext context)
            {
                var modelType = typeof(T);
                var modelState = new ModelStateDictionary();
                var modelMetadata = modelMetadataProvider.GetMetadataForType(modelType);
                var actionDescriptor = new ActionDescriptor();
                var actionContext = new ActionContext(context, context.GetRouteData(), actionDescriptor, modelState);
                var valueProvider = await CompositeValueProvider.CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories);

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

                modelBindingContext.Model = Activator.CreateInstance(modelType);
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

                return (T)modelBindingContext.Result.Model;
            }
        }


        class TestStrinHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.AString = content?.SelectToken("AString")?.Value<string>();
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
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStrinHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.EnableWriteSupport();
                    });
                    services.AddRouting();
                    services.AddControllers(options => { });
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            var model = new RequestModel();
            model.AString = expectedString;
            model.ANumber = expectedNumber;
            var json = (string) JsonConvert.SerializeObject(model);
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

    internal class JsonBodyValueProvider : IValueProvider
    {
        private readonly JObject jObject;

        public JsonBodyValueProvider(JObject jObject)
        {
            this.jObject = jObject;
        }

        public bool ContainsPrefix(string prefix)
        {
            return false;
        }

        public ValueProviderResult GetValue(string key)
        {
            var token = jObject.SelectToken(key);

            return new ValueProviderResult(token.ToString());
        }
    }

    class RequestModel
    {
        public string AString { get; set; }
        public int ANumber { get; set; }
    }

    class RequestWrapper
    {
        [FromBody]
        public RequestModel Body { get; set; }
    }

    class IntegerRequestModel
    {
        public int ANumber { get; set; }
    }

    class StringRequestModel
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