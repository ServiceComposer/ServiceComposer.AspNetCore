using System;
using System.Text.Json;
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
    public class When_using_custom_serialization_settings
    {
        class ResponseHandler : ICompositionRequestsHandler
        {
            [HttpGet("/empty-response/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();
                vm.RequestId = ctx.RequestId;

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_invoke_custom_settings_provider()
        {
            var invokedUseCustomJsonSerializerSettings = false;
            var expectedComposedRequestId = Guid.NewGuid().ToString();

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ResponseHandler>();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.ResponseSerialization.UseCustomJsonSerializerSettings(request =>
                        {
                            invokedUseCustomJsonSerializerSettings = true;
                            return new JsonSerializerOptions();
                        });
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(invokedUseCustomJsonSerializerSettings);
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.RequestId);
        }

        [Fact]
        public async Task Should_throw_if_camel_case_request_and_no_valid_contract_resolver()
        {
            async Task Function()
            {
                // Arrange
                var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>(configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ResponseHandler>();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.CamelCase;
                        options.ResponseSerialization.UseCustomJsonSerializerSettings(request => new JsonSerializerOptions());
                    });
                    services.AddRouting();
                }, configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }).CreateClient();

                // Act
                await client.GetAsync("/empty-response/1");
            }
            await Assert.ThrowsAsync<ArgumentException>(Function);
        }

        [Fact]
        public async Task Should_not_throw_if_camel_case_request_and_valid_contract_resolver()
        {
            var invokedUseCustomJsonSerializerSettings = false;
            var expectedComposedRequestId = Guid.NewGuid().ToString();

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ResponseHandler>();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.CamelCase;
                        options.ResponseSerialization.UseCustomJsonSerializerSettings(request =>
                        {
                            invokedUseCustomJsonSerializerSettings = true;
                            return new JsonSerializerOptions()
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                            };
                        });
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(invokedUseCustomJsonSerializerSettings);
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.requestId);
        }

        [Fact]
        public async Task Should_use_default_serialization_settings_if_custom_provider_returns_null()
        {
            var invokedUseCustomJsonSerializerSettings = false;
            var expectedComposedRequestId = Guid.NewGuid().ToString();

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_matching_handler>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<ResponseHandler>();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.CamelCase;
                        options.ResponseSerialization.UseCustomJsonSerializerSettings(request =>
                        {
                            invokedUseCustomJsonSerializerSettings = true;
                            return null;
                        });
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var response = await client.GetAsync("/empty-response/1");

            // Assert
            Assert.True(invokedUseCustomJsonSerializerSettings);
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.requestId);
        }
    }
}