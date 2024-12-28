#nullable enable
using System;
using System.Collections.Generic;
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
    public class When_using_endpoint_filters
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
        
        class SampleEndpointFilter : IEndpointFilter
        {
            public bool Invoked { get; private set; }
            public object? CapturedResponse { get; private set; }
            
            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                Invoked = true;
                CapturedResponse = await next(context);

                return CapturedResponse;
            }
        }
        
        class AnotherSampleEndpointFilter : IEndpointFilter
        {
            public bool Invoked { get; private set; }
            public object? CapturedResponse { get; private set; }

            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                Invoked = true;
                CapturedResponse = await next(context);

                return CapturedResponse;
            }
        }
        
        [Fact]
        public async Task Should_invoke_the_filter()
        {
            var expectedComposedRequestId = Guid.NewGuid().ToString();
            var sampleEndpointFilter = new SampleEndpointFilter();
            var anotherSampleEndpointFilter = new AnotherSampleEndpointFilter();

            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_using_endpoint_filters>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.RegisterCompositionHandler<ResponseHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder =>
                    {
                        builder.MapCompositionHandlers()
                            .AddEndpointFilter(sampleEndpointFilter)
                            .AddEndpointFilter(anotherSampleEndpointFilter);
                    });
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var response = await client.GetAsync("/empty-response/1");

            Assert.True(response.IsSuccessStatusCode);
            
            Assert.True(sampleEndpointFilter.Invoked);
            Assert.Equal(expectedComposedRequestId, (sampleEndpointFilter.CapturedResponse as dynamic)?.RequestId);

            Assert.True(anotherSampleEndpointFilter.Invoked);
            Assert.Equal(expectedComposedRequestId, (anotherSampleEndpointFilter.CapturedResponse as dynamic)?.RequestId);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.RequestId);
        }
        
        class ResponseHandlerWithModelBinding : ICompositionRequestsHandler
        {
            [HttpPost("/empty-response/{id}")]
            [Model(type: typeof(ModelValues))]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();
                vm.RequestId = ctx.RequestId;

                return Task.CompletedTask;
            }
        }

        class ModelValues
        {
            [FromRoute(Name = "id")]
            public int Identifier { get; set; }
            
            [FromBody]
            public MyClass? Body { get; set; }
        }

        class MyClass
        {
            public required string Text { get; set; }
        }

        class CaptureArgumentsEndpointFilter : IEndpointFilter
        {
            public bool Invoked { get; private set; }
            public IList<object?> CapturedArguments { get; private set; } = null!;

            public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                Invoked = true;
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

            var modelBody = new MyClass(){ Text = "some text" };
            var json = JsonConvert.SerializeObject(modelBody);
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;
            
            // Act
            var response = await client.PostAsync("/empty-response/1", stringContent);

            Assert.True(response.IsSuccessStatusCode);
            Assert.True(captureArgumentsEndpointFilter.Invoked);
            
            var arguments = captureArgumentsEndpointFilter.CapturedArguments;
            Assert.NotNull(arguments);
            Assert.True(arguments.Count == 1);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.RequestId);
        }
        
        // TODO: test using multiple Model attributes
        // TODO: test using value types
        // TODO: test using different biding sources
    }
}