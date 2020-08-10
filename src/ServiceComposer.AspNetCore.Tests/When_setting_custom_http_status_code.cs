using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Gateway;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_setting_custom_http_status_code
    {
        class CustomStatusCodeHandler : IHandleRequests
        {
            public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
            {
                var response = request.HttpContext.Response;
                response.StatusCode = (int)HttpStatusCode.Forbidden;

                return Task.CompletedTask;
            }

            public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
            {
                var controller = routeData.Values["controller"]?.ToString();
                return controller?.ToLowerInvariant() == "custom-status-code";
            }
        }

        [Fact]
        public async Task Default_status_code_should_be_overwritten()
        {
            // Arrange
            var expectedStatusCode = HttpStatusCode.Forbidden;
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_setting_custom_http_status_code>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterRequestsHandler<CustomStatusCodeHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.RunCompositionGatewayWithDefaultRoutes();
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/custom-status-code/1");

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }
    }
}