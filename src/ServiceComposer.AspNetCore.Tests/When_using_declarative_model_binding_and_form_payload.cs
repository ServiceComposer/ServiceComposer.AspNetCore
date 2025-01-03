#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_declarative_model_binding_and_form_payload
    {
        class ResponseHandlerWithModelBinding : ICompositionRequestsHandler
        {
            [HttpPost("/empty-response/{id}")]
            [BindFromForm<MyClass>()]
            [BindFromRoute<int>(routeValueKey: "id")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();
                vm.RequestId = ctx.RequestId;

                return Task.CompletedTask;
            }
        }

        class MyClass
        {
            public required int Number { get; init; }
            public required string Text { get; init; }
        }

        class CaptureArgumentsEndpointFilter : IEndpointFilter
        {
            public IList<object?> CapturedArguments { get; private set; } = null!;

            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                CapturedArguments = context.Arguments;
                return await next(context);
            }
        }
        
        [Fact]
        public async Task Should_populate_arguments_as_expected()
        {
            var expectedComposedRequestId = Guid.NewGuid().ToString();
            const string expectedText = "some text";
            const int expectedNumber = 42;
            var captureArgumentsEndpointFilter = new CaptureArgumentsEndpointFilter();

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_endpoint_filters>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.RegisterCompositionHandler<ResponseHandlerWithModelBinding>();
                    });
                    services.AddRouting();
                    services.AddControllers();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapCompositionHandlers()
                            .AddEndpointFilter(captureArgumentsEndpointFilter);
                    });
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var formData = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("text", expectedText),
                new KeyValuePair<string, string>("number", expectedNumber.ToString())
            ]);
            var response = await client.PostAsync("/empty-response/1", formData);

            Assert.True(response.IsSuccessStatusCode);
            
            var arguments = captureArgumentsEndpointFilter.CapturedArguments;
            Assert.NotNull(arguments);
            Assert.True(arguments.Count == 2);
            
            var myClass = arguments.OfType<MyClass>().Single();
            Assert.Equal(expectedText, myClass.Text);
            Assert.Equal(expectedNumber, myClass.Number);
        }
    }
}