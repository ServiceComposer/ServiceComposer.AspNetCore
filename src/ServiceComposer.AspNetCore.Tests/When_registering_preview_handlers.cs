using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_registering_preview_handlers
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        class TestPreviewHandler : IViewModelPreviewHandler
        {
            public bool Invoked { get; set; }

            public Task Preview(dynamic viewModel)
            {
                Invoked = true;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task ViewModel_is_intercepted_as_expected()
        {
            // Arrange
            var previewHandler = new TestPreviewHandler();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_2_handlers>
            (
                configureServices: services =>
                {
                    services.AddSingleton<IViewModelPreviewHandler>(previewHandler);
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
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
            Assert.True(previewHandler.Invoked);
        }
    }
}