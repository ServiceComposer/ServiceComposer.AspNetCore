using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
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
    public class When_disabling_write_support
    {
        const string WarningFormat = "Write support is disabled, so the write routes of {ComponentCount} composition component(s) aren't mapped, " +
                                     "and requests to them get a 404, or a 405 if a GET composition handler shares the path: {UnmappedWriteRoutes}. " +
                                     "Remove options.DisableWriteSupport(), or the components declaring them.";

        class WriteOnlyHandler : ICompositionRequestsHandler
        {
            [HttpPost("/write-only")]
            [HttpDelete("/write-only/{id}")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class ReadHandler : ICompositionRequestsHandler
        {
            [HttpGet("/read-and-write")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        class WriteHandler : ICompositionRequestsHandler
        {
            [HttpPut("/read-and-write")]
            public Task Handle(HttpRequest request) => Task.CompletedTask;
        }

        static (HttpClient Client, ITestLoggerFactory LoggerFactory) CreateClient(bool disableWriteSupport, bool registerWriteHandlers = true)
        {
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ReadHandler>();
                        if (registerWriteHandlers)
                        {
                            options.RegisterCompositionHandler<WriteOnlyHandler>();
                            options.RegisterCompositionHandler<WriteHandler>();
                        }

                        if (disableWriteSupport)
                        {
                            options.DisableWriteSupport();
                        }
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

            return (client, loggerFactory);
        }

        static StringContent EmptyJson() => new("{}", Encoding.UTF8, MediaTypeNames.Application.Json);

        static LogEntry[] Warnings(ITestLoggerFactory loggerFactory) => loggerFactory.Sink.LogEntries
            .Where(entry =>
            {
                try { return entry.OriginalFormat == WarningFormat; }
                catch { return false; }
            })
            .ToArray();

        [Fact]
        public async Task A_path_with_only_write_handlers_returns_not_found()
        {
            var (client, _) = CreateClient(disableWriteSupport: true);

            var response = await client.PostAsync("/write-only", EmptyJson());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task A_path_with_a_read_handler_returns_method_not_allowed()
        {
            var (client, _) = CreateClient(disableWriteSupport: true);

            var response = await client.PutAsync("/read-and-write", EmptyJson());

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(["GET"], response.Content.Headers.Allow);
        }

        [Fact]
        public async Task A_warning_lists_the_unmapped_write_routes()
        {
            var (client, loggerFactory) = CreateClient(disableWriteSupport: true);

            await client.GetAsync("/read-and-write");

            var warning = Assert.Single(Warnings(loggerFactory));
            Assert.Equal(LogLevel.Warning, warning.LogLevel);
            Assert.Equal(2, warning.Properties.Single(p => p.Key == "ComponentCount").Value);
            Assert.Equal("DELETE /write-only/{id} (WriteOnlyHandler), POST /write-only (WriteOnlyHandler), PUT /read-and-write (WriteHandler)",
                warning.Properties.Single(p => p.Key == "UnmappedWriteRoutes").Value);
        }

        [Fact]
        public async Task No_warning_is_logged_without_write_handlers()
        {
            var (client, loggerFactory) = CreateClient(disableWriteSupport: true, registerWriteHandlers: false);

            await client.GetAsync("/read-and-write");

            Assert.Empty(Warnings(loggerFactory));
        }

        [Fact]
        public async Task No_warning_is_logged_with_write_support_enabled()
        {
            var (client, loggerFactory) = CreateClient(disableWriteSupport: false);

            var response = await client.PostAsync("/write-only", EmptyJson());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(Warnings(loggerFactory));
        }
    }
}
