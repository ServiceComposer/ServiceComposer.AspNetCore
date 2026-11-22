#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_declarative_model_binding_fails
    {
        class Invocations
        {
            int handlers;
            int filters;
            public int Handlers => handlers;
            public int Filters => filters;
            public void Handler() => Interlocked.Increment(ref handlers);
            public void Filter() => Interlocked.Increment(ref filters);
        }

        class Payload
        {
            public int Number { get; set; }
        }

        class RouteHandler : ICompositionRequestsHandler
        {
            [HttpGet("/items/{id}")]
            [HttpPost("/items/{id}")]
            [BindFromRoute<int>(routeValueKey: "id")]
            public Task Handle(HttpRequest request)
            {
                request.HttpContext.RequestServices.GetRequiredService<Invocations>().Handler();
                return Task.CompletedTask;
            }
        }

        class BodyHandler : ICompositionRequestsHandler
        {
            [HttpPost("/items/{id}")]
            [BindFromBody<Payload>]
            public Task Handle(HttpRequest request)
            {
                request.HttpContext.RequestServices.GetRequiredService<Invocations>().Handler();
                return Task.CompletedTask;
            }
        }

        static (HttpClient Client, Invocations Invocations) CreateClient(
            Action<ViewModelCompositionOptions>? configure = null,
            Action<IServiceCollection>? configureServices = null)
        {
            var invocations = new Invocations();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<RouteHandler>();
                        options.RegisterCompositionHandler<BodyHandler>();
                        configure?.Invoke(options);
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddSingleton(invocations);
                    configureServices?.Invoke(services);
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers()
                        .AddEndpointFilter(async (context, next) =>
                        {
                            invocations.Filter();
                            return await next(context);
                        }));
                }
            ).CreateClient();

            return (client, invocations);
        }

        static StringContent Json(string json)
        {
            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            content.Headers.ContentLength = Encoding.UTF8.GetByteCount(json);
            return content;
        }

        static async Task<JsonObject> AssertBadRequest(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
            Assert.Equal(400, problem["status"]!.GetValue<int>());
            return problem;
        }

        [Fact]
        public async Task A_value_that_cant_be_converted_returns_bad_request_without_running_handlers_or_filters()
        {
            var (client, invocations) = CreateClient();

            var problem = await AssertBadRequest(await client.GetAsync("/items/not-a-number"));

            Assert.NotNull(problem["errors"]!["id"]);
            Assert.Equal(0, invocations.Handlers);
            Assert.Equal(0, invocations.Filters);
        }

        [Fact]
        public async Task Malformed_json_returns_bad_request()
        {
            var (client, invocations) = CreateClient();

            await AssertBadRequest(await client.PostAsync("/items/1", Json("{ \"number\": ")));

            Assert.Equal(0, invocations.Handlers);
        }

        [Fact]
        public async Task Errors_of_all_the_handlers_are_merged()
        {
            var (client, _) = CreateClient();

            var problem = await AssertBadRequest(await client.PostAsync("/items/not-a-number", Json("{ \"number\": ")));

            var errors = problem["errors"]!.AsObject();
            Assert.NotNull(errors["id"]);
            Assert.True(errors.Count > 1, $"Expected the body errors too, got: {errors.ToJsonString()}");
        }

        [Fact]
        public async Task A_valid_request_runs_the_handlers()
        {
            var (client, invocations) = CreateClient();

            var response = await client.PostAsync("/items/1", Json("{ \"number\": 42 }"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, invocations.Handlers);
        }

        [Fact]
        public async Task Handlers_run_when_failing_on_binding_errors_is_disabled()
        {
            var (client, invocations) = CreateClient(options => options.ModelBinding.FailOnBindingErrors = false);

            var response = await client.GetAsync("/items/not-a-number");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, invocations.Handlers);
        }

        [Fact]
        public async Task The_problem_details_service_writes_the_response()
        {
            var (client, _) = CreateClient(configureServices: services => services.AddProblemDetails(options =>
                options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions["customized"] = true));

            var problem = await AssertBadRequest(await client.GetAsync("/items/not-a-number"));

            Assert.True(problem["customized"]!.GetValue<bool>());
            Assert.NotNull(problem["errors"]!["id"]);
        }
    }
}
