using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using System.Threading.Tasks;
using Xunit;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class __When_a_matching_handler_is_found
    {
        class CatchAllMatchingHandler : IHandleRequests
        {
            [HttpGet("/matching-handlers/{id}")]
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
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/matching-handlers/1");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
