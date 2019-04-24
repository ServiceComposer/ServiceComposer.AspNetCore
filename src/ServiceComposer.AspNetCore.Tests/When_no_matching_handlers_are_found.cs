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
    public class When_no_matching_handlers_are_found
    {
        class NeverMatchingHandler : IHandleRequests
        {
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                throw new System.NotImplementedException();
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                return false;
            }
        }

        [Fact]
        public async Task Should_return_404()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_no_matching_handlers_are_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.DisableAssemblyScanning();
                        options.RegisterRequestsHandler<NeverMatchingHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/no-matching-handlers/1");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
