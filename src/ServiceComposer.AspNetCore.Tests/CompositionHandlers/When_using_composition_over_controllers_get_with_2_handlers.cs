using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlers
{
    namespace Controllers
    {
        [Route("/api/CompositionOverControllerUsingCompositionHandlers")]
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
        public class Model
        {
            [FromRoute]public int id { get; set; }
        }

        public record AnEvent
        {
            
        }
 
        [CompositionHandler]
        public class CaseInsensitiveRouteTestGetIntegerCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/api/compositionovercontrollerusingcompositionhandlers/{id}")]
            public Task Handle(Model model)
            {
                var vm = httpContextAccessor.HttpContext.Request.GetComposedResponseModel();
                vm.ANumber = model.id;
                
                return Task.CompletedTask;
            }
        }

        [CompositionHandler]
        public class CaseSensitiveRouteTestGetIntegerCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/api/CompositionOverControllerUsingCompositionHandlers/{id}")]
            public Task Handle(Model model)
            {
                var vm = httpContextAccessor.HttpContext.Request.GetComposedResponseModel();
                vm.ANumber = model.id;

                return Task.CompletedTask;
            }
        }

        [CompositionHandler]
        public class TestGetStringCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/api/CompositionOverControllerUsingCompositionHandlers/{id}")]
            public Task Handle()
            {
                var request = httpContextAccessor.HttpContext!.Request;
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                
                return request.GetCompositionContext().RaiseEvent(new AnEvent());
            }
        }
        
        public class AnEventSubscriber : ICompositionEventsHandler<AnEvent>
        {
            public Task Handle(AnEvent @event, HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.HandledAnEvent = true;
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
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_TestGetStringCompositionHandler_Handle>();
                        options.RegisterCompositionHandler<CaseSensitiveRouteTestGetIntegerCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_CaseSensitiveRouteTestGetIntegerCompositionHandler_Handle_ServiceComposer_AspNetCore_Tests_CompositionHandlers_When_using_composition_over_controllers_get_with_2_handlers_Model_model>();
                        options.EnableCompositionOverControllers();
                    });
                    services.AddHttpContextAccessor();
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
            var response = await client.GetAsync("/api/compositionovercontrollerusingcompositionhandlers/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
        
        [Fact]
        public async Task Returns_expected_response_handling_anEvent()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_TestGetStringCompositionHandler_Handle>();
                        options.RegisterCompositionHandler<CaseSensitiveRouteTestGetIntegerCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_CaseSensitiveRouteTestGetIntegerCompositionHandler_Handle_ServiceComposer_AspNetCore_Tests_CompositionHandlers_When_using_composition_over_controllers_get_with_2_handlers_Model_model>();
                        options.RegisterCompositionHandler<AnEventSubscriber>();
                        options.EnableCompositionOverControllers();
                    });
                    services.AddHttpContextAccessor();
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
            var response = await client.GetAsync("/api/compositionovercontrollerusingcompositionhandlers/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.True(responseObj.SelectToken("HandledAnEvent")?.Value<bool>());
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
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_TestGetStringCompositionHandler_Handle>();
                        options.RegisterCompositionHandler<CaseInsensitiveRouteTestGetIntegerCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_CaseInsensitiveRouteTestGetIntegerCompositionHandler_Handle_ServiceComposer_AspNetCore_Tests_CompositionHandlers_When_using_composition_over_controllers_get_with_2_handlers_Model_model>();
                        options.EnableCompositionOverControllers(useCaseInsensitiveRouteMatching: true);
                    });
                    services.AddHttpContextAccessor();
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
            var response = await client.GetAsync("/api/compositionovercontrollerUsingCompositionHandlers/1");

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
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler>();
                        options.RegisterCompositionHandler<CaseSensitiveRouteTestGetIntegerCompositionHandler>();
                        options.RegisterCompositionHandler<CaseInsensitiveRouteTestGetIntegerCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_TestGetStringCompositionHandler_Handle>();
                        options.RegisterCompositionHandler<Generated.When_using_composition_over_controllers_get_with_2_handlers_CaseInsensitiveRouteTestGetIntegerCompositionHandler_Handle_ServiceComposer_AspNetCore_Tests_CompositionHandlers_When_using_composition_over_controllers_get_with_2_handlers_Model_model>();
                    });
                    services.AddRouting();
                    services.AddHttpContextAccessor();
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
                _ = await client.GetAsync("/api/CompositionOverControllerUsingCompositionHandlers/1");
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
