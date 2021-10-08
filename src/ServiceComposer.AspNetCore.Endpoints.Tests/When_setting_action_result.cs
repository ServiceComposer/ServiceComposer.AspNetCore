using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_setting_action_result
    {
        class TestGetIntegerHandler : ICompositionRequestsHandler
        {
            class Model
            {
                [FromRoute]public int id { get; set; }
            }

            [HttpGet("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var model = await request.Bind<Model>();

                var problems = new ValidationProblemDetails(new Dictionary<string, string[]>() 
                {
                    { "Id", new []{ "I'm not sure I like the Id property value" } }
                });
                var result = new BadRequestObjectResult(problems);

                request.SetActionResult(result);
            }
        }

        class TestGetStringHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Returns_expected_bad_request_using_output_formatters()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetStringHandler>();
                        options.RegisterCompositionHandler<TestGetIntegerHandler>();
                        options.ResponseSerialization.UseOutputFormatters = true;
                    });
                    services.AddRouting();
                    services.AddControllers()
                        .AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.False(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }

        [Fact]
        public async Task Throws_if_output_formatters_are_not_enabled()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => 
            {
                // Arrange
                var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterCompositionHandler<TestGetStringHandler>();
                            options.RegisterCompositionHandler<TestGetIntegerHandler>();
                            options.ResponseSerialization.UseOutputFormatters = false;
                        });
                        services.AddRouting();
                        services.AddControllers()
                            .AddNewtonsoftJson();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder => builder.MapCompositionHandlers());
                    }
                ).CreateClient();

                // Act
                var response = await client.GetAsync("/sample/1");
            });
        }
    }
}