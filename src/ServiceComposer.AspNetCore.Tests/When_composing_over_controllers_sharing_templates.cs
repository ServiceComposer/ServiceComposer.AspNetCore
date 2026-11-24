using System;
using System.Dynamic;
using System.Net;
using System.Net.Http;
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
        [Route("/api/shared-templates")]
        public class SharedTemplatesController : ControllerBase
        {
            static IActionResult Model()
            {
                dynamic model = new ExpandoObject();
                model.FromController = true;
                return new OkObjectResult(model);
            }

            // Composition sets the model of results that have none
            [HttpGet("get-and-post")]
            public IActionResult GetOnGetAndPost() => new OkObjectResult(null);

            [HttpPost("get-and-post")]
            public IActionResult PostOnGetAndPost() => Model();

            [HttpPost("post-only")]
            public IActionResult PostOnly() => Model();

            [HttpGet("other-data-sources")]
            public IActionResult OtherDataSources() => new OkObjectResult(null);
        }
    }

    public class When_composing_over_controllers_sharing_templates
    {
        class GetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/api/shared-templates/get-and-post")]
            [HttpGet("/api/shared-templates/post-only")]
            [HttpGet("/api/shared-templates/other-data-sources")]
            public Task Handle(HttpRequest request)
            {
                request.GetComposedResponseModel().FromHandler = true;
                return Task.CompletedTask;
            }
        }

        static HttpClient CreateClient(bool mapOtherEndpointsFirst = false)
        {
            return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<GetHandler>();
                    });
                    services.AddRouting();
                    services.AddControllers().AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        if (mapOtherEndpointsFirst)
                        {
                            builder.MapGet("/api/shared-templates/minimal-api", () => "minimal API");
                        }

                        builder.MapControllers().WithCompositionOverControllers();
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();
        }

        static async Task<JObject> Body(HttpResponseMessage response) =>
            JObject.Parse(await response.Content.ReadAsStringAsync());

        static bool? Value(JObject body, string name) =>
            body.GetValue(name, StringComparison.OrdinalIgnoreCase)?.Value<bool>();

        [Fact]
        public async Task Handlers_compose_over_the_action_with_the_same_http_method()
        {
            var client = CreateClient();

            var getResponse = await client.GetAsync("/api/shared-templates/get-and-post");
            var postResponse = await client.PostAsync("/api/shared-templates/get-and-post", null);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(Value(await Body(getResponse), "FromHandler"));

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var postBody = await Body(postResponse);
            Assert.True(Value(postBody, "FromController"));
            Assert.Null(Value(postBody, "FromHandler"));
        }

        [Fact]
        public async Task Handlers_get_their_own_endpoint_when_the_controller_has_no_action_for_their_http_method()
        {
            var client = CreateClient();

            var getResponse = await client.GetAsync("/api/shared-templates/post-only");
            var postResponse = await client.PostAsync("/api/shared-templates/post-only", null);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var getBody = await Body(getResponse);
            Assert.True(Value(getBody, "FromHandler"));
            Assert.Null(Value(getBody, "FromController"));

            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
            var postBody = await Body(postResponse);
            Assert.True(Value(postBody, "FromController"));
            Assert.Null(Value(postBody, "FromHandler"));
        }

        [Fact]
        public async Task Handlers_compose_over_controllers_mapped_after_other_endpoints()
        {
            var client = CreateClient(mapOtherEndpointsFirst: true);

            var response = await client.GetAsync("/api/shared-templates/other-data-sources");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(Value(await Body(response), "FromHandler"));
        }
    }
}
