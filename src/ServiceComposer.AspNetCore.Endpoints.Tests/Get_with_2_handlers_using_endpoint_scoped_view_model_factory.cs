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
    public class Get_with_2_handlers_using_endpoint_scoped_view_model_factory
    {
        class TestViewModel
        {
            public int ANumber { get; set; }
            public string AString { get; set; }
        }

        class TestGetIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var routeData = request.HttpContext.GetRouteData();
                var vm = request.GetComposedResponseModel<TestViewModel>();
                vm.ANumber = int.Parse(routeData.Values["id"].ToString());
                return Task.CompletedTask;
            }
        }

        class TestGetStrinHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel<TestViewModel>();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        class TestFactory : IEndpointScopedViewModelFactory
        {
            [HttpGet("/sample/{id}")]
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return new TestViewModel();
            }
        }

        [Fact]
        public async Task Uses_defined_factory()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStrinHandler>();
                        options.RegisterCompositionHandler<TestGetIntegerHandler>();
                        options.RegisterEndpointScopedViewModelFactory<TestFactory>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }
}