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
        [Route("api/sample")]
        public class SampleApiController : Controller
        {
            [HttpGet("{id}")]
            public Task<int> Get(int id)
            {
                return Task.FromResult(id);
            }
        }
    }

    public class When_using_composition_and_Mvc
    {
        class TestGetIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
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
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Both_composition_endpoint_and_Mvc_endpoint_return_expected_values()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_composition_and_Mvc>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStrinHandler>();
                        options.RegisterCompositionHandler<TestGetIntegerHandler>();
                    });
                    services.AddControllers();
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapCompositionHandlers();
                        builder.MapControllers();
                    });
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");

            // Act
            var composedResponse = await client.GetAsync("/sample/1");
            var apiResponse = await client.GetAsync("/api/sample/32");

            // Assert
            Assert.True(composedResponse.IsSuccessStatusCode);
            Assert.True(apiResponse.IsSuccessStatusCode);

            var responseObj = JObject.Parse(await composedResponse.Content.ReadAsStringAsync());

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());

            var apiResponsObj = await apiResponse.Content.ReadAsStringAsync();
            Assert.Equal(32, int.Parse(apiResponsObj));
        }
    }
}