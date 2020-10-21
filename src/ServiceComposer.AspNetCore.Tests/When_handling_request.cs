using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_handling_request
    {
        class CatchAllMatchingHandler : IHandleRequests
        {
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                vm.RequestId = requestId;

                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return true;
            }
        }

        [Fact]
        public async Task Request_header_should_be_not_null_if_not_explicitly_set()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterRequestsHandler<CatchAllMatchingHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/matching-handlers/1");

            // Assert
            response.EnsureSuccessStatusCode();

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.NotNull(body.requestId);
        }

        [Fact]
        public async Task Request_header_should_be_set_as_expected()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterRequestsHandler<CatchAllMatchingHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            var expectedRequestId = "my-request";
            client.DefaultRequestHeaders.Add(ComposedRequestIdHeader.Key, expectedRequestId);

            // Act
            var response = await client.GetAsync("/matching-handlers/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedRequestId, (string)body.requestId);
        }
    }
}
