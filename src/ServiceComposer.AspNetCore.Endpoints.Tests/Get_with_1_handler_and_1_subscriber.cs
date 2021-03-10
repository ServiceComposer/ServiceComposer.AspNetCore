﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class Get_with_1_handler_and_1_subscriber
    {
        class TestEvent{}

        class TestGetHandlerThatAppendAStringAndRaisesTestEvent : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";

                await vm.RaiseEvent(new TestEvent());
            }
        }

        class TestGetSubscriberThatAppendAnotherStringWhenTestEventIsRaised : ICompositionEventsSubscriber
        {
            [HttpGet("/sample/{id}")]
            public void Subscribe(ICompositionEventsPublisher publisher)
            {
                publisher.Subscribe<TestEvent>((@event, request) =>
                {
                    var vm = request.GetComposedResponseModel();
                    vm.AnotherString = "sample";
                    return Task.CompletedTask;
                });
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_1_handler_and_1_subscriber>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandlerThatAppendAStringAndRaisesTestEvent>();
                        options.RegisterCompositionHandler<TestGetSubscriberThatAppendAnotherStringWhenTestEventIsRaised>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal("sample", responseObj?.SelectToken("AnotherString")?.Value<string>());
        }

        [Fact]
        public async Task Returns_expected_response_using_output_formatters()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Get_with_1_handler_and_1_subscriber>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandlerThatAppendAStringAndRaisesTestEvent>();
                        options.RegisterCompositionHandler<TestGetSubscriberThatAppendAnotherStringWhenTestEventIsRaised>();
                        options.ResponseSerialization.UseOutputFormatters = true;
                    });
                    services.AddRouting();
                    services.AddControllers().AddNewtonsoftJson();
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
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal("sample", responseObj?.SelectToken("AnotherString")?.Value<string>());
        }
    }
}