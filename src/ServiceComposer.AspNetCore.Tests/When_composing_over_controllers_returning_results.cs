using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    namespace Controllers
    {
        public class ProductModel
        {
            public string Name { get; set; }
            public decimal Price { get; set; }
        }

        [Route("/api/controller-results")]
        public class ControllerResultsController : ControllerBase
        {
            [HttpGet("dynamic-model/{id}")]
            public IActionResult DynamicModel(int id)
            {
                dynamic model = new ExpandoObject();
                model.Name = "From the controller";
                return Ok(model);
            }

            [HttpGet("typed-model/{id}")]
            public ProductModel TypedModel(int id)
            {
                return new ProductModel { Name = "From the controller" };
            }

            [HttpGet("no-model/{id}")]
            public IActionResult NoModel(int id)
            {
                return new ObjectResult(null);
            }

            [HttpGet("not-found/{id}")]
            public IActionResult NotFoundWithProblem(int id)
            {
                return NotFound(new ProblemDetails { Title = "Not found by the controller" });
            }

            [HttpGet("redirect/{id}")]
            public IActionResult RedirectElsewhere(int id)
            {
                return Redirect("/elsewhere");
            }

            [HttpPost("post-redirect/{id}")]
            public IActionResult PostAndRedirect(int id)
            {
                return RedirectToAction(nameof(NoModel), new { id });
            }

            [HttpGet("ok/{id}")]
            public IActionResult OkWithoutModel(int id)
            {
                return Ok();
            }

            [HttpGet("not-found-without-body/{id}")]
            public IActionResult NotFoundWithoutBody(int id)
            {
                return NotFound();
            }
        }
    }

    public class When_composing_over_controllers_returning_results
    {
        class HandlerInvocations
        {
            public int Count;
        }

        class DynamicModelHandler(HandlerInvocations invocations) : ICompositionRequestsHandler
        {
            [HttpGet("/api/controller-results/dynamic-model/{id}")]
            [HttpGet("/api/controller-results/no-model/{id}")]
            [HttpGet("/api/controller-results/not-found/{id}")]
            [HttpGet("/api/controller-results/redirect/{id}")]
            [HttpGet("/api/controller-results/ok/{id}")]
            [HttpGet("/api/controller-results/not-found-without-body/{id}")]
            [HttpPost("/api/controller-results/post-redirect/{id}")]
            public Task Handle(HttpRequest request)
            {
                Interlocked.Increment(ref invocations.Count);
                var vm = request.GetComposedResponseModel();
                vm.Price = 42m;
                return Task.CompletedTask;
            }
        }

        class TypedModelHandler(HandlerInvocations invocations) : ICompositionRequestsHandler
        {
            [HttpGet("/api/controller-results/typed-model/{id}")]
            public Task Handle(HttpRequest request)
            {
                Interlocked.Increment(ref invocations.Count);
                var vm = request.GetComposedResponseModel<Controllers.ProductModel>();
                vm.Price = 42m;
                return Task.CompletedTask;
            }
        }

        static (HttpClient Client, HandlerInvocations Invocations) CreateClient()
        {
            var invocations = new HandlerInvocations();
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddSingleton(invocations);
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<DynamicModelHandler>();
                        options.RegisterCompositionHandler<TypedModelHandler>();
                    });
                    services.AddRouting();
                    services.AddControllers().AddNewtonsoftJson();
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
            ).CreateClient(new() { AllowAutoRedirect = false });

            return (client, invocations);
        }

        // Newtonsoft camel cases typed models' properties, but not dynamic models' members
        static T Value<T>(JObject body, string name) => body.GetValue(name, StringComparison.OrdinalIgnoreCase)!.Value<T>();

        [Fact]
        public async Task Handlers_compose_into_the_dynamic_model_returned_by_the_controller()
        {
            var (client, _) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/dynamic-model/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("From the controller", Value<string>(body, "Name"));
            Assert.Equal(42m, Value<decimal>(body, "Price"));
        }

        [Fact]
        public async Task Handlers_compose_into_the_typed_model_returned_by_the_controller()
        {
            var (client, _) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/typed-model/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("From the controller", Value<string>(body, "Name"));
            Assert.Equal(42m, Value<decimal>(body, "Price"));
        }

        [Fact]
        public async Task Handlers_compose_into_a_new_view_model_when_the_controller_returns_none()
        {
            var (client, _) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/no-model/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(42m, Value<decimal>(body, "Price"));
            Assert.Null(body.GetValue("Name", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Error_results_are_not_composed()
        {
            var (client, invocations) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/not-found/1");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Not found by the controller", Value<string>(body, "title"));
            Assert.Null(body.GetValue("Price", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(0, invocations.Count);
        }

        [Fact]
        public async Task Error_results_without_a_body_are_not_composed()
        {
            var (client, invocations) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/not-found-without-body/1");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, invocations.Count);
        }

        [Fact]
        public async Task Redirects_are_composed_and_the_response_is_the_redirect()
        {
            var (client, invocations) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/redirect/1");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/elsewhere", response.Headers.Location?.ToString());
            Assert.Equal(1, invocations.Count);
        }

        [Fact]
        public async Task Post_handlers_run_before_the_controller_redirects()
        {
            var (client, invocations) = CreateClient();

            var response = await client.PostAsync("/api/controller-results/post-redirect/1", null);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/api/controller-results/no-model/1", response.Headers.Location?.ToString());
            Assert.Equal(1, invocations.Count);
        }

        [Fact]
        public async Task Results_without_a_model_are_composed_and_the_response_is_the_controllers()
        {
            var (client, invocations) = CreateClient();

            var response = await client.GetAsync("/api/controller-results/ok/1");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            Assert.Equal(1, invocations.Count);
        }
    }
}
