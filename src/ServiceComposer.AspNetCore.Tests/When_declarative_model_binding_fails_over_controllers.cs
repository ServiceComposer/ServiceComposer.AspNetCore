#nullable enable
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    namespace BindingErrorsControllers
    {
        [Route("/api/binding-errors-over-controllers")]
        public class BindingErrorsController : ControllerBase
        {
            [HttpGet("{id}")]
            public object Get(string id)
            {
                HttpContext.RequestServices.GetRequiredService<When_declarative_model_binding_fails_over_controllers.Log>().Add("action");
                return new { };
            }
        }
    }

    public class When_declarative_model_binding_fails_over_controllers
    {
        public class Log
        {
            readonly ConcurrentQueue<string> entries = new();
            public void Add(string entry) => entries.Enqueue(entry);
            public string[] Entries => entries.ToArray();
        }

        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/api/binding-errors-over-controllers/{id}")]
            [BindFromRoute<int>(routeValueKey: "id")]
            public Task Handle(HttpRequest request)
            {
                request.HttpContext.RequestServices.GetRequiredService<Log>().Add("handler");
                return Task.CompletedTask;
            }
        }

        static (HttpClient Client, Log Log) CreateClient(Action<ViewModelCompositionOptions>? configure = null)
        {
            var log = new Log();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<Handler>();
                        configure?.Invoke(options);
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddSingleton(log);
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapControllers()
                            .AddEndpointFilter(async (context, next) =>
                            {
                                var result = await next(context);
                                if (result is IStatusCodeHttpResult { StatusCode: { } statusCode })
                                {
                                    log.Add($"outer filter saw {statusCode}");
                                }

                                return result;
                            })
                            .WithCompositionOverControllers();
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            return (client, log);
        }

        [Fact]
        public async Task Returns_bad_request_before_running_the_action_and_the_handlers()
        {
            var (client, log) = CreateClient();

            var response = await client.GetAsync("/api/binding-errors-over-controllers/not-a-number");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            Assert.NotNull(problem["errors"]!["id"]);
            Assert.Equal(["outer filter saw 400"], log.Entries);
        }

        [Fact]
        public async Task A_valid_request_runs_the_action_and_the_handlers()
        {
            var (client, log) = CreateClient();

            var response = await client.GetAsync("/api/binding-errors-over-controllers/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["action", "handler"], log.Entries);
        }

        [Fact]
        public async Task The_action_and_the_handlers_run_when_failing_on_binding_errors_is_disabled()
        {
            var (client, log) = CreateClient(options => options.ModelBinding.FailOnBindingErrors = false);

            var response = await client.GetAsync("/api/binding-errors-over-controllers/not-a-number");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["action", "handler"], log.Entries);
        }
    }
}
