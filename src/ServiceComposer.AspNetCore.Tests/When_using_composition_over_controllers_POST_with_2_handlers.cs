using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    namespace Controllers
    {
        [Route("/api/CompositionOverControllerPost")]
        public class CompositionOverControllerPostController : ControllerBase
        {
            [HttpPost("{id}")]
            public Task<object> Post(int id)
            {
                return Task.FromResult((object)null);
            }
        }
    }

    public class When_using_composition_over_controllers_POST_with_2_handlers
    {
        class CaseInsensitiveRoute_TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/api/compositionovercontrollerpost/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.ANumber = content?.SelectToken("ANumber")?.Value<int>();
            }
        }

        class TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/api/CompositionOverControllerPost/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.ANumber = content?.SelectToken("ANumber")?.Value<int>();
            }
        }

        class TestStringHandler : ICompositionRequestsHandler
        {
            [HttpPost("/api/CompositionOverControllerPost/{id}")]
            public async Task Handle(HttpRequest request)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true );
                var body = await reader.ReadToEndAsync();
                var content = JObject.Parse(body);

                var vm = request.GetComposedResponseModel();
                vm.AString = content?.SelectToken("AString")?.Value<string>();
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            var expectedString = "this is a string value";
            var expectedNumber = 32;

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStringHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                        options.EnableCompositionOverControllers();
                        options.EnableWriteSupport();
                    });
                    services.AddRouting();
                    services.AddControllers()
                            .AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapControllers();
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            dynamic model = new ExpandoObject();
            model.AString = expectedString;
            model.ANumber = expectedNumber;

            var json = (string) JsonConvert.SerializeObject(model);
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            var response = await client.PostAsync("/api/CompositionOverControllerPost/1", stringContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal(expectedString, responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(expectedNumber, responseObj?.SelectToken("ANumber")?.Value<int>());
        }

        // [Fact]
        // public async Task Returns_expected_response_with_case_insensitive_routes()
        // {
        //     // Arrange
        //     var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
        //     (
        //         configureServices: services =>
        //         {
        //             services.AddViewModelComposition(options =>
        //             {
        //                 options.AssemblyScanner.Disable();
        //                 options.RegisterCompositionHandler<TestGetStringHandler>();
        //                 options.RegisterCompositionHandler<CaseInsensitiveRoute_TestGetIntegerHandler>();
        //                 options.EnableCompositionOverControllers(useCaseInsensitiveRouteMatching: true);
        //             });
        //             services.AddRouting();
        //             services.AddControllers()
        //                 .AddNewtonsoftJson();
        //         },
        //         configure: app =>
        //         {
        //             app.UseRouting();
        //             app.UseEndpoints(builder =>
        //             {
        //                 builder.MapControllers();
        //                 builder.MapCompositionHandlers();
        //             });
        //         }
        //     ).CreateClient();
        //
        //     // Act
        //     var response = await client.GetAsync("/api/compositionovercontroller/1");
        //
        //     // Assert
        //     Assert.True(response.IsSuccessStatusCode);
        //
        //     var responseString = await response.Content.ReadAsStringAsync();
        //     var responseObj = JObject.Parse(responseString);
        //
        //     Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
        //     Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        // }
        //
        // [Fact]
        // public async Task Fails_if_composition_over_controllers_is_disabled()
        // {
        //     // Arrange
        //     var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
        //     (
        //         configureServices: services =>
        //         {
        //             services.AddViewModelComposition(options =>
        //             {
        //                 options.AssemblyScanner.Disable();
        //                 options.RegisterCompositionHandler<TestGetStringHandler>();
        //                 options.RegisterCompositionHandler<CaseInsensitiveRoute_TestGetIntegerHandler>();
        //             });
        //             services.AddRouting();
        //             services.AddControllers()
        //                 .AddNewtonsoftJson();
        //         },
        //         configure: app =>
        //         {
        //             app.UseRouting();
        //             app.UseEndpoints(builder =>
        //             {
        //                 builder.MapControllers();
        //                 builder.MapCompositionHandlers();
        //             });
        //         }
        //     ).CreateClient();
        //
        //     Exception capturedException = null;
        //     try
        //     {
        //         // Act
        //         var response = await client.GetAsync("/api/CompositionOverController/1");
        //     }
        //     catch (Exception e)
        //     {
        //         capturedException = e;
        //     }
        //
        //     // Assert
        //     Assert.NotNull(capturedException);
        //     Assert.Equal("Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException", capturedException.GetType().FullName);
        // }
    }
}
