using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_endpoints
    {
        class EmptyResponseHandler : IHandleRequests
        {
            [HttpGet("/empty-response/{id}")]
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                throw new System.NotImplementedException();
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
                        options.RegisterRequestsHandler<EmptyResponseHandler>();
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
            response.EnsureSuccessStatusCode();
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
                        options.RegisterRequestsHandler<EmptyResponseHandler>();
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

        class AppendIntegerHandler : IHandleRequests
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                vm.ANumber = int.Parse(routeData.Values["id"].ToString());
                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                throw new System.NotImplementedException();
            }
        }

        class AppendStrinHandler : IHandleRequests
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                vm.AString = "sample";
                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                throw new System.NotImplementedException();
            }
        }

        [Fact]
        public async Task Get_returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterRequestsHandler<AppendStrinHandler>();
                        options.RegisterRequestsHandler<AppendIntegerHandler>();
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
