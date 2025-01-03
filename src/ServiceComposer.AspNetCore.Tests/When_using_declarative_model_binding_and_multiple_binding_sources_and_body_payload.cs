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
    public class When_using_declarative_model_binding_and_multiple_binding_sources_and_body_payload
    {
        class ResponseHandlerWithModelBinding : ICompositionRequestsHandler
        {
            [HttpPost("/empty-response/{id}")]
            [Bind<MyClass>()]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                var ctx = request.GetCompositionContext();
                vm.RequestId = ctx.RequestId;
                
#pragma warning disable SC0001
                var myClass = ctx.GetArguments(this).Argument<MyClass>();
                vm.NumberFromHeader = myClass?.Number;
                vm.SomeTextFromComplexType = myClass?.AComplexType.SomeText;
#pragma warning restore SC0001

                return Task.CompletedTask;
            }
        }

        class MyClass
        {
            [FromRoute(Name = "id")]
            public required int Identifier { get; init; }

            [FromHeader(Name = "X-Number")]
            public required int Number { get; init; }

            [FromBody]
            public required ComplexType AComplexType { get; init; }
        }

        class ComplexType
        {
            public required string SomeText { get; init; }
            public required string SomeMoreText { get; init; }
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
            const string expectedComplexTypeSomeText = "complex type some text";
            const string expectedComplexTypeSomeMoreText = "complex type some more text";
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
            client.DefaultRequestHeaders.Add("X-Number", expectedNumber.ToString());
            
            // Act
            var json = JsonConvert.SerializeObject(new ComplexType(){ SomeText = expectedComplexTypeSomeText, SomeMoreText = expectedComplexTypeSomeMoreText});
            var jsonContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            jsonContent.Headers.ContentLength = json.Length;

            var response = await client.PostAsync("/empty-response/1", jsonContent);

            Assert.True(response.IsSuccessStatusCode);
            
            var contentString = await response.Content.ReadAsStringAsync();
            dynamic responseBody = JObject.Parse(contentString);
            Assert.Equal(expectedComposedRequestId, (string)responseBody.RequestId);
            Assert.Equal(expectedNumber, (int)responseBody.NumberFromHeader);
            Assert.Equal(expectedComplexTypeSomeText, (string)responseBody.SomeTextFromComplexType);
            
            var arguments = captureArgumentsEndpointFilter.CapturedArguments;
            Assert.NotNull(arguments);
            Assert.True(arguments.Count == 1);
            
            var myClass = arguments.OfType<MyClass>().Single();
            Assert.Equal(1, myClass.Identifier);
            Assert.Equal(expectedNumber, myClass.Number);
            Assert.Equal(expectedComplexTypeSomeText, myClass.AComplexType.SomeText);
            Assert.Equal(expectedComplexTypeSomeMoreText, myClass.AComplexType.SomeMoreText);
        }
    }
}