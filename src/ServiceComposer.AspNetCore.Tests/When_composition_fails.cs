using System;
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
    public class When_composition_fails
    {
        class ThrowingHandler : ICompositionRequestsHandler
        {
            [HttpGet("/failing/{id}")]
            public Task Handle(HttpRequest request)
            {
                throw new InvalidOperationException("Something went wrong during composition");
            }
        }

        [Fact]
        public async Task Logs_error_when_handler_throws()
        {
            // Arrange
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Error));

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ThrowingHandler>();
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
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/failing/1"));

            // Assert
            var log = Assert.Single(loggerFactory.Sink.LogEntries);
            Assert.Equal(LogLevel.Error, log.LogLevel);
            Assert.Equal("Composition failed for request {RequestId}.", log.OriginalFormat);
            Assert.Single(log.Properties, p => p.Key == "RequestId");
            Assert.NotNull(log.Exception);
            Assert.IsType<InvalidOperationException>(log.Exception);
        }
    }
}
