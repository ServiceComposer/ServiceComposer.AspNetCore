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
    public class When_handling_composition_request_logs
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/sample/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class TestPostHandler : ICompositionRequestsHandler
        {
            [HttpPost("/api/sample/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        [Fact]
        public async Task Logs_debug_on_get_request()
        {
            // Arrange
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
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

            // Act
            await client.GetAsync("/api/sample/1");

            // Assert
            var log = loggerFactory.Sink.LogEntries
                .Single(l =>
                {
                    try { return l.OriginalFormat == "Handling composition request at {Method} {Template} with {HandlerCount} handler(s)."; }
                    catch { return false; }
                });
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal("GET", log.Properties.Single(p => p.Key == "Method").Value);
            Assert.Equal("api/sample/{id}", log.Properties.Single(p => p.Key == "Template").Value);
            Assert.Equal(1, log.Properties.Single(p => p.Key == "HandlerCount").Value);
        }

        [Fact]
        public async Task Logs_debug_on_post_request()
        {
            // Arrange
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestPostHandler>();
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

            // Act
            await client.PostAsync("/api/sample/1", content: null);

            // Assert
            var log = loggerFactory.Sink.LogEntries
                .Single(l =>
                {
                    try { return l.OriginalFormat == "Handling composition request at {Method} {Template} with {HandlerCount} handler(s)."; }
                    catch { return false; }
                });
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal("POST", log.Properties.Single(p => p.Key == "Method").Value);
            Assert.Equal("api/sample/{id}", log.Properties.Single(p => p.Key == "Template").Value);
            Assert.Equal(1, log.Properties.Single(p => p.Key == "HandlerCount").Value);
        }
    }
}
