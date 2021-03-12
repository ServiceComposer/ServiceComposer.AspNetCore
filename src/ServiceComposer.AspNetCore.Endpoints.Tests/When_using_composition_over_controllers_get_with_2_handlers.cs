using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    namespace Controllers
    {
        [Route("/api/CompositionOverController")]
        public class SampleController : ControllerBase
        {
            [HttpGet("{id}")]
            public Task<object> Get(int id)
            {
                return Task.FromResult((object)null);
            }
        }
    }

    public class When_using_composition_over_controllers_get_with_2_handlers
    {
        class Model
        {
            [FromRoute]public int id { get; set; }
        }
        class CaseInsensitiveRoute_TestGetIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/compositionovercontroller/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<Model>();
                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.id;
            }
        }

        class CaseSensitiveRoute_TestGetIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/CompositionOverController/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<Model>();
                var vm = request.GetComposedResponseModel();
                vm.ANumber = model.id;
            }
        }

        class TestGetStringHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/CompositionOverController/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStringHandler>();
                        options.RegisterCompositionHandler<CaseSensitiveRoute_TestGetIntegerHandler>();
                        options.EnableCompositionOverControllers();
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

            // Act
            var response = await client.GetAsync("/api/compositionovercontroller/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }

        [Fact]
        public async Task Returns_expected_response_with_case_insensitive_routes()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStringHandler>();
                        options.RegisterCompositionHandler<CaseInsensitiveRoute_TestGetIntegerHandler>();
                        options.EnableCompositionOverControllers(useCaseInsensitiveRouteMatching: true);
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

            // Act
            var response = await client.GetAsync("/api/compositionovercontroller/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }

        [Fact]
        public async Task Fails_if_composition_over_controllers_is_disabled()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStringHandler>();
                        options.RegisterCompositionHandler<CaseInsensitiveRoute_TestGetIntegerHandler>();
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

            Exception capturedException = null;
            try
            {
                // Act
                var response = await client.GetAsync("/api/CompositionOverController/1");
            }
            catch (Exception e)
            {
                capturedException = e;
            }

            // Assert
            Assert.NotNull(capturedException);
            Assert.Equal("Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException", capturedException.GetType().FullName);
        }
    }
}
