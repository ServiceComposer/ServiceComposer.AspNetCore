using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_setting_custom_http_status_code
    {
        public class CustomStatusCodeHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var response = request.HttpContext.Response;
                response.StatusCode = (int)HttpStatusCode.Forbidden;

                return Task.CompletedTask;
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
                        options.RegisterCompositionHandler<CustomStatusCodeHandler>();
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

            // Act
            var composedResponse = await client.GetAsync("/sample/1");

            // Assert
            Assert.Equal(expectedStatusCode, composedResponse.StatusCode);
        }
    }
}