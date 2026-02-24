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
    namespace CompositionOverControllersLogsControllers
    {
        [Route("/api/LogsController")]
        public class LogsController : ControllerBase
        {
            [HttpGet("{id}")]
            public Task<object> Get(int id) => Task.FromResult((object)null);
        }
    }

    public class When_using_composition_over_controllers_logs
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/LogsController/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Logs_debug_when_components_are_registered_over_a_controller()
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
                        options.EnableCompositionOverControllers();
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapControllers();
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            // Assert — log emitted at startup, no HTTP request needed
            var log = Assert.Single(loggerFactory.Sink.LogEntries,
                l => l.OriginalFormat == "{ComponentCount} composition component(s) registered to compose over the controller at GET {Template}.");
            Assert.Equal(LogLevel.Debug, log.LogLevel);
            Assert.Equal(1, log.Properties.Single(p => p.Key == "ComponentCount").Value);
            Assert.Equal("api/logscontroller/{id}", log.Properties.Single(p => p.Key == "Template").Value);
        }
    }
}
