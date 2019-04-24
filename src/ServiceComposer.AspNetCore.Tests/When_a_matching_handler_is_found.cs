using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_a_matching_handler_is_found
    {
        class CatchAllMatchingHandler : IHandleRequests
        {
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return true;
            }
        }

        [Fact]
        public async Task Should_return_success_code()
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
        }
    }
}
