using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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
    namespace Controllers
    {
        // A route of its own: composition over controllers routes are shared by all test hosts in 4.x
        [Route("/api/no-composition-handlers")]
        public class NoCompositionHandlersController : ControllerBase
        {
            [HttpGet("{id}")]
            public Task<object> Get(int id)
            {
                return Task.FromResult((object)null);
            }
        }
    }

    public class When_routes_have_no_composition_handlers
    {
        const string WarningFormat = "No composition handler is registered for {HttpMethods} /{Template}, so the route isn't mapped, " +
                                     "and these components never run for it: {Components}. Check the template for typos, or add a composition handler for the route.";

        class OnlySubscriber : ICompositionEventsSubscriber
        {
            [HttpGet("/only-subscriber/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher) { }
        }

        // Matches Controllers.NoCompositionHandlersController, whose action returns null: a 204
        class OnlySubscriberOverController : ICompositionEventsSubscriber
        {
            [HttpGet("/api/no-composition-handlers/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher) { }
        }

        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/with-handler/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class SubscriberWithHandler : ICompositionEventsSubscriber
        {
            [HttpGet("/with-handler/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher) { }
        }

        class PreviewsCounter : IViewModelPreviewHandler
        {
            public int Count;

            public Task Preview(HttpRequest request)
            {
                Interlocked.Increment(ref Count);
                return Task.CompletedTask;
            }
        }

        static (HttpClient Client, ITestLoggerFactory LoggerFactory, PreviewsCounter Previews) CreateClient(bool composeOverControllers = false)
        {
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));
            var previews = new PreviewsCounter();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<OnlySubscriber>();
                        options.RegisterCompositionHandler<Handler>();
                        options.RegisterCompositionHandler<SubscriberWithHandler>();
                        if (composeOverControllers)
                        {
                            options.RegisterCompositionHandler<OnlySubscriberOverController>();
                        }
                        if (composeOverControllers)
                        {
                            options.EnableCompositionOverControllers();
                        }
                        options.Services.AddSingleton<IViewModelPreviewHandler>(previews);
                    });
                    services.AddRouting();
                    if (composeOverControllers)
                    {
                        services.AddControllers();
                    }
                    services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        if (composeOverControllers)
                        {
                            builder.MapControllers();
                        }
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            return (client, loggerFactory, previews);
        }

        static LogEntry[] Warnings(ITestLoggerFactory loggerFactory) => loggerFactory.Sink.LogEntries
            .Where(entry =>
            {
                try { return entry.OriginalFormat == WarningFormat; }
                catch { return false; }
            })
            .ToArray();

        [Fact]
        public async Task A_route_with_only_subscribers_is_not_mapped()
        {
            var (client, loggerFactory, previews) = CreateClient();

            var response = await client.GetAsync("/only-subscriber/1");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());
            Assert.Equal(0, previews.Count);

            var warning = Assert.Single(Warnings(loggerFactory));
            Assert.Equal("GET", warning.Properties.Single(p => p.Key == "HttpMethods").Value);
            Assert.Equal("only-subscriber/{id}", warning.Properties.Single(p => p.Key == "Template").Value);
            Assert.Equal(nameof(OnlySubscriber), warning.Properties.Single(p => p.Key == "Components").Value);
        }

        [Fact]
        public async Task A_route_with_handlers_and_subscribers_is_mapped()
        {
            var (client, _, previews) = CreateClient();

            var response = await client.GetAsync("/with-handler/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, previews.Count);
        }

        [Fact]
        public async Task Only_subscribers_matching_a_controller_route_leave_the_controller_response_alone()
        {
            var (client, loggerFactory, previews) = CreateClient(composeOverControllers: true);

            var response = await client.GetAsync("/api/no-composition-handlers/1");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, previews.Count);
            Assert.Contains(Warnings(loggerFactory), warning =>
                (string)warning.Properties.Single(p => p.Key == "Components").Value == nameof(OnlySubscriberOverController));
        }
    }
}
