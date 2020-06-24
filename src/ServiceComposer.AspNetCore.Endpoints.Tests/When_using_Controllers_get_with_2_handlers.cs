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
    
    public class When_using_Controllers_get_with_2_handlers
    {
        class TestGetIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/CompositionOverController/{id}")]
            public Task Handle(HttpRequest request)
            {
                var routeData = request.HttpContext.GetRouteData();
                var vm = request.GetComposedResponseModel();
                vm.ANumber = int.Parse(routeData.Values["id"].ToString());
                return Task.CompletedTask;
            }
        }

        class TestGetStrinHandler : ICompositionRequestsHandler
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
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStrinHandler>();
                        options.RegisterCompositionHandler<TestGetIntegerHandler>();
                    });
                    services.AddRouting();
                    services.AddControllers();
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
            var response = await client.GetAsync("/api/CompositionOverController/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }
}