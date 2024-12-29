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
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_declarative_model_binding
    {
        class ResponseHandlerWithModelBinding : ICompositionRequestsHandler
        {
            [HttpPost("/empty-response/{id}")]
            [BindModelFromBody<MyClass>]
            [BindModelFromRoute<int>(routeValueKey: "id")]
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

            var json = JsonConvert.SerializeObject(new MyClass(){ Text = "some text" });
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;
            
            // Act
            var response = await client.PostAsync("/empty-response/1", stringContent);

            Assert.True(response.IsSuccessStatusCode);
            
            var arguments = captureArgumentsEndpointFilter.CapturedArguments;
            Assert.NotNull(arguments);
            Assert.True(arguments.Count == 2);
            
            var myClass = arguments.OfType<MyClass>().Single();
            Assert.Equal("some text", myClass.Text);
        }
        
        // TODO: test using multiple Model attributes
        // TODO: test using value types
        // TODO: test using different biding sources
    }
}