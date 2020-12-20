using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_registering_view_model_factory
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        class TestViewModelFactory : IViewModelFactory
        {
            public bool Invoked { get; private set; }
            public DynamicViewModel CreateViewModel(HttpContext httpContext, CompositionContext compositionContext)
            {
                Invoked = true;
                var logger = httpContext.RequestServices.GetRequiredService<ILogger<DynamicViewModel>>();
                return new DynamicViewModel(logger, compositionContext);
            }
        }

        [Fact]
        public async Task ViewModel_is_created_using_custom_factory()
        {
            // Arrange
            var factory = new TestViewModelFactory();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_registering_view_model_factory>
            (
                configureServices: services =>
                {
                    services.AddSingleton<IViewModelFactory>(factory);
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
            Assert.True(factory.Invoked);
        }
    }
}