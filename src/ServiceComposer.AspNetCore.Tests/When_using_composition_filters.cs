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
        //[SampleCompositionRequestFilterOnClass, AnotherSampleCompositionRequestFilterOnClass]
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
        
        class SampleCompositionRequestFilterAttribute : CompositionRequestFilterAttribute
        {
            public override async ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                await next(context);

                var vm = context.HttpContext.Request.GetComposedResponseModel();
                vm.InvokedSampleCompositionRequestFilterAttribute = true;

                return vm;
            }
        }
        
        // class SampleCompositionRequestFilterOnClassAttribute : CompositionRequestFilterAttribute
        // {
        //     public override async ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        //     {
        //         await next(context);
        //
        //         var vm = context.HttpContext.Request.GetComposedResponseModel();
        //         vm.InvokedSampleCompositionRequestFilterOnClassAttribute = true;
        //
        //         return vm;
        //     }
        // }
        //
        // class AnotherSampleCompositionRequestFilterOnClassAttribute : CompositionRequestFilterAttribute
        // {
        //     public override async ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        //     {
        //         await next(context);
        //
        //         var vm = context.HttpContext.Request.GetComposedResponseModel();
        //         vm.InvokedAnotherSampleCompositionRequestFilterOnClassAttribute = true;
        //         
        //         return vm;
        //     }
        // }
        
        class InvokedSampleCompositionRequestFilterInterface : ICompositionRequestFilter<ResponseHandler>
        {
            public async ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
            {
                await next(context);

                var vm = context.HttpContext.Request.GetComposedResponseModel();
                vm.InvokedSampleCompositionRequestFilterInterface = true;
                
                return vm;
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
                        options.RegisterCompositionRequestsFilter(typeof(InvokedSampleCompositionRequestFilterInterface));
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
            Assert.True((bool)body.InvokedSampleCompositionRequestFilterAttribute);
            Assert.True((bool)body.InvokedSampleCompositionRequestFilterInterface);
            // Assert.True((bool)body.InvokedSampleCompositionRequestFilterOnClassAttribute);
            // Assert.True((bool)body.InvokedAnotherSampleCompositionRequestFilterOnClassAttribute);
        }
    }
}