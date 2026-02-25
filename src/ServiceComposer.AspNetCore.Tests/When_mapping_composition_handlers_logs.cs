using System.Linq;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_mapping_composition_handlers_logs
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/sample/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class TestGetHandler2 : ICompositionRequestsHandler
        {
            [HttpGet("/api/other/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        [Fact]
        public void Logs_debug_component_count_at_startup()
        {
            // Arrange
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

            _ = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
                        options.RegisterCompositionHandler<TestGetHandler2>();
                    });
                    services.AddRouting();
                    services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Assert — log emitted at startup, no HTTP request needed
            var log = Assert.Single(loggerFactory.Sink.LogEntries,
                l => l.OriginalFormat == "{ComponentCount} composition component(s) registered.");
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(2, log.Properties.Single(p => p.Key == "ComponentCount").Value);
        }
    }
}
