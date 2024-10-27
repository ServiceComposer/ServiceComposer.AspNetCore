using System;
using System.Collections.Generic;
using System.Net;
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
    public class When_setting_action_result
    {
        const string expectedError = "I'm not sure I like the Id property value";

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
                    { "Id", new []{ expectedError } }
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
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic responseObj = JObject.Parse(responseString);

            dynamic errors = responseObj.errors;
            var idErrors = (JArray)errors["Id"];

            var error = idErrors[0].Value<string>();

            Assert.Equal(expectedError, error);
        }

        [Fact]
        public async Task Throws_if_output_formatters_are_not_enabled()
        {
            async Task Function()
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
                _ = await client.GetAsync("/sample/1");
            }
            await Assert.ThrowsAsync<NotSupportedException>(Function);
        }
    }
}