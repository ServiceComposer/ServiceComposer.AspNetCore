#nullable enable
using System;
using System.Linq;
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
    public class When_using_declarative_model_binding_and_services
    {
        interface ITestService
        {
            string GetValue();
        }

        class TestService : ITestService
        {
            public string GetValue() => "value-from-service";
        }

        class ResponseHandlerWithServiceBinding : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            [BindFromRoute<int>(routeValueKey: "id")]
            [BindFromServices<ITestService>("testService")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();

#pragma warning disable SC0001
                var id = ctx.GetArguments(this).Argument<int>("id");
                var testService = ctx.GetArguments(this).Argument<ITestService>("testService");
#pragma warning restore SC0001

                vm.Id = id;
                vm.ServiceValue = testService?.GetValue();

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_resolve_service_from_di_container()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_endpoint_filters>
            (
                configureServices: services =>
                {
                    services.AddSingleton<ITestService, TestService>();
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.RegisterCompositionHandler<ResponseHandlerWithServiceBinding>();
                    });
                    services.AddRouting();
                    services.AddControllers();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/42");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic responseBody = JObject.Parse(contentString);
            Assert.Equal(42, (int)responseBody.Id);
            Assert.Equal("value-from-service", (string)responseBody.ServiceValue);
        }
    }
}
