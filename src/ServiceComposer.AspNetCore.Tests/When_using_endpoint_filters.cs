#nullable enable
using System;
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
            public bool Invoked { get; set; }
            public object CapturedResponse { get; set; }
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
    }
}