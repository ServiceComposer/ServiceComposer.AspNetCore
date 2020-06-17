#if NETCOREAPP3_1

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.When_using_endpoints
{
    public class Get_with_no_matching_handlers
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
        public async Task Return_404()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_no_matching_handlers>
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
    }
}

#endif