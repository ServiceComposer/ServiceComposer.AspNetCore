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
    public class When_using_composition_filters
    {
        [SampleCompositionRequestFilter, AnotherSampleCompositionRequestFilter]
        class ResponseHandler : ICompositionRequestsHandler
        {
            [HttpGet("/empty-response/{id}"), SampleCompositionRequestFilter]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();
                vm.RequestId = ctx.RequestId;

                return Task.CompletedTask;
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        abstract class CompositionRequestFilterAttribute : Attribute
        {
            
        }

        class CompositionRequestContext
        {
            public HttpRequest Request { get; }
        }

        interface ICompositionRequestFilter<T>
        {
            ValueTask<object> InvokeAsync(CompositionRequestContext context, CompositionDelegate next);
        }
        
        class SampleCompositionRequestFilterAttribute : CompositionRequestFilterAttribute
        {
            public bool Invoked { get; set; }
            public object CapturedResponse { get; set; }
            
            public async ValueTask<object> InvokeAsync(CompositionRequestContext context, CompositionDelegate next)
            {
                Invoked = true;
                CapturedResponse = await next(context);

                return CapturedResponse;
            }
        }
        
        class AnotherSampleCompositionRequestFilterAttribute : CompositionRequestFilterAttribute
        {
            public bool Invoked { get; set; }
            public object CapturedResponse { get; set; }
            
            public async ValueTask<object> InvokeAsync(CompositionRequestContext context, CompositionDelegate next)
            {
                Invoked = true;
                CapturedResponse = await next(context);

                return CapturedResponse;
            }
        }
        
        class SampleCompositionRequestFilter : ICompositionRequestFilter<ResponseHandler>
        {
            public bool Invoked { get; set; }
            public object CapturedResponse { get; set; }
            
            public async ValueTask<object> InvokeAsync(ICompositionContext context, CompositionDelegate next)
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
                        builder.MapCompositionHandlers();
                    });
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("composed-request-id", expectedComposedRequestId);

            // Act
            var response = await client.GetAsync("/empty-response/1");

            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic body = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)body.RequestId);
        }
    }
}