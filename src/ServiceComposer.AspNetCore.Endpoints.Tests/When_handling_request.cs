using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_handling_request
    {
        class EmptyResponseHandler : ICompositionRequestsHandler
        {
            [HttpGet("/empty-response/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.RequestId = request.Headers.GetComposedRequestId();

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Request_header_should_be_not_null_if_not_explicitly_set()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>
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

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.NotNull(body.requestId);
        }

        [Fact]
        public async Task Request_header_should_be_set_as_expected()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>
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

            var expectedRequestId = "my-request";
            client.DefaultRequestHeaders.Add(ComposedRequestIdHeader.Key, expectedRequestId);
            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedRequestId, (string)body.requestId);
        }
    }
}