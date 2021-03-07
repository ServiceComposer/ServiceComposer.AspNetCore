using System;
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
            private IOptions<JsonOptions> jsonOptions;

            public TestIntegerHandler(IModelBinderFactory modelBinderFactory,
                IModelMetadataProvider modelMetadataProvider, IOptions<JsonOptions> jsonOptions)
            {
                this.modelBinderFactory = modelBinderFactory;
                this.modelMetadataProvider = modelMetadataProvider;
                this.jsonOptions = jsonOptions;
            }

            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await Bind<IntegerRequestModel>(request.HttpContext);
                
                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.ANumber; 
            }

            async Task<T> Bind<T>(HttpContext context)
            {
                var routeProvider = new RouteValueProvider(BindingSource.Path, context.Request.RouteValues);
                var queryProvider = new QueryStringValueProvider(BindingSource.Query, context.Request.Query,
                    CultureInfo.InvariantCulture);
                var compositeValueProvider = new CompositeValueProvider
                {
                    routeProvider,
                    queryProvider
                };
                IValueProvider formProvider = null;
                
                if (context.Request.HasFormContentType)
                {
                    formProvider = new FormValueProvider(BindingSource.Form, context.Request.Form,
                        CultureInfo.CurrentCulture);
                    ;
                    compositeValueProvider.Add(formProvider);
                }
                else
                {
                    var request = context.Request;
                    request.Body.Position = 0;
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    var content = JObject.Parse(body);
                    compositeValueProvider.Add(new JsonBodyValueProvider(content));
                }
                
                var modelMetadata = modelMetadataProvider.GetMetadataForType(typeof(T));
            
                // .NET 5 only
                // if (modelMetadata.BoundConstructor != null)
                // {
                //     throw new NotSupportedException("Record type not supported");
                // }
            
                var modelState = new ModelStateDictionary();
            
                var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                    new ActionContext(context, context.GetRouteData(), new ActionDescriptor(), modelState),
                    compositeValueProvider,
                    modelMetadata,
                    bindingInfo: null,
                    modelName: "");
            
                modelBindingContext.Model = Activator.CreateInstance(typeof(T));
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
            
                    // We're using the model metadata as the cache token here so that TryUpdateModelAsync calls
                    // for the same model type can share a binder. This won't overlap with normal model binding
                    // operations because they use the ParameterDescriptor for the token.
                    CacheToken = modelMetadata,
                };
                var binder = modelBinderFactory.CreateBinder(factoryContext);
            
                await binder.BindModelAsync(modelBindingContext);
            
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

    class IntegerRequestModel
    {
        public int ANumber { get; set; }
    }

    class StringRequestModel
    {
        public string AString { get; set; }
    }
}