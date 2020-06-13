using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_endpoints
    {
        class EmptyResponseHandler : ICompositionRequestsHandler
        {
            [HttpGet("/empty-response/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Matching_handler_with_attribute_is_found()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<EmptyResponseHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task No_matching_handlers_return_404()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<EmptyResponseHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/not-valid/1");

            // Assert
            Assert.Equal( HttpStatusCode.NotFound, response.StatusCode);
        }

        class AppendIntegerHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var routeData = request.HttpContext.GetRouteData();
                var vm = request.GetResponseModel();
                vm.ANumber = int.Parse(routeData.Values["id"].ToString());
                return Task.CompletedTask;
            }
        }

        class AppendStrinHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task A_simple_get_with_2_handlers_returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<AppendStrinHandler>();
                        options.RegisterCompositionHandler<AppendIntegerHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseObj = JObject.Parse(responseString);
            Assert.Equal("sample", responseObj.AString);
            Assert.Equal(1, responseObj.ANumber);
        }
    }
}
