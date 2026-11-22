#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
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
    namespace BindFailsControllers
    {
        [Route("/api/bind-fails-over-controllers")]
        public class BindFailsController : ControllerBase
        {
            [HttpGet("{id}")]
            public object Get(string id) => new { };
        }
    }

    public class When_bind_fails
    {
        public class Model
        {
            [FromRoute] public int id { get; set; }
        }

        class BindHandler : ICompositionRequestsHandler
        {
            [HttpGet("/bind/{id}")]
            [HttpGet("/api/bind-fails-over-controllers/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<Model>();
                var vm = request.GetComposedResponseModel();
                vm.Id = model.id;
            }
        }

        class TryBindHandler : ICompositionRequestsHandler
        {
            [HttpGet("/try-bind/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var (_, _, modelState) = await request.TryBind<Model>();
                var vm = request.GetComposedResponseModel();
                vm.ErrorCount = modelState.ErrorCount;
            }
        }

        static HttpClient CreateClient(Action<ViewModelCompositionOptions>? configure = null, ILoggerFactory? loggerFactory = null)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<BindHandler>();
                        options.RegisterCompositionHandler<TryBindHandler>();
                        configure?.Invoke(options);
                    });
                    services.AddRouting();
                    services.AddControllers();
                    if (loggerFactory != null)
                    {
                        services.Replace(ServiceDescriptor.Singleton(loggerFactory));
                    }
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapControllers().WithCompositionOverControllers();
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();
        }

        static async Task AssertBadRequest(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            Assert.NotNull(problem["errors"]!["id"]);
        }

        [Fact]
        public async Task Returns_bad_request()
        {
            await AssertBadRequest(await CreateClient().GetAsync("/bind/not-a-number"));
        }

        [Fact]
        public async Task Returns_bad_request_with_composition_over_controllers()
        {
            await AssertBadRequest(await CreateClient().GetAsync("/api/bind-fails-over-controllers/not-a-number"));
        }

        [Fact]
        public async Task Returns_the_model_when_failing_on_binding_errors_is_disabled()
        {
            var client = CreateClient(options => options.ModelBinding.FailOnBindingErrors = false);

            var response = await client.GetAsync("/bind/not-a-number");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            Assert.Equal(0, body["id"]!.GetValue<int>());
        }

        [Fact]
        public async Task TryBind_returns_the_errors_without_throwing()
        {
            var response = await CreateClient().GetAsync("/try-bind/not-a-number");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
            Assert.Equal(1, body["errorCount"]!.GetValue<int>());
        }

        [Fact]
        public async Task Logs_the_binding_failure_as_debug_not_as_an_error()
        {
            var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

            await CreateClient(loggerFactory: loggerFactory).GetAsync("/bind/not-a-number");

            Assert.DoesNotContain(loggerFactory.Sink.LogEntries, l => l.LogLevel >= LogLevel.Error);
            Assert.Contains(loggerFactory.Sink.LogEntries, l =>
            {
                try { return l.OriginalFormat == "Model binding failed for request {RequestId}." && l.LogLevel == LogLevel.Debug; }
                catch { return false; }
            });
        }
    }
}
