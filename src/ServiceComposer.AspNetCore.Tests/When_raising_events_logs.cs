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
    public class When_raising_events_logs
    {
        class SampleEvent { }

        class TestGetHandlerThatRaisesEvent : ICompositionRequestsHandler
        {
            [HttpGet("/api/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var ctx = request.GetCompositionContext();
                await ctx.RaiseEvent(new SampleEvent());
            }
        }

        class TestGetSubscriber : ICompositionEventsSubscriber
        {
            [HttpGet("/api/sample/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<SampleEvent>((_, _) => Task.CompletedTask);
            }
        }

        [Fact]
        public async Task Logs_debug_when_event_is_raised()
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
                        options.RegisterCompositionHandler<TestGetHandlerThatRaisesEvent>();
                        options.RegisterCompositionHandler<TestGetSubscriber>();
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
            var response = await client.GetAsync("/api/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var log = loggerFactory.Sink.LogEntries
                .Single(l =>
                {
                    try { return l.OriginalFormat == "Raising event {EventType} to {HandlerCount} handler(s)."; }
                    catch { return false; }
                });
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal("SampleEvent", log.Properties.Single(p => p.Key == "EventType").Value);
            Assert.Equal(1, log.Properties.Single(p => p.Key == "HandlerCount").Value);
        }
    }
}
