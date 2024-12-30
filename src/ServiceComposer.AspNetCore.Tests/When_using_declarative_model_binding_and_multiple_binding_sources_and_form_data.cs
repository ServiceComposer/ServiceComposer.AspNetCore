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
    public class When_using_declarative_model_binding_and_multiple_binding_sources_and_form_data
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

                return Task.CompletedTask;
            }
        }

        class MyClass
        {
            [FromRoute(Name = "id")]
            public required int Identifier { get; init; }

            [FromForm(Name = "text")]
            public required string Text { get; init; }
            
            [FromHeader(Name = "X-Number")]
            public required int Number { get; init; }

            [FromForm(Name = "json_data")]
            public required IFormCollection AComplexType { get; init; }
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
            const string expectedText = "some text";
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
            using var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent(expectedText), "text");
            
            var json = JsonConvert.SerializeObject(new ComplexType(){ SomeText = expectedComplexTypeSomeText, SomeMoreText = expectedComplexTypeSomeMoreText});
            var jsonContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            jsonContent.Headers.ContentLength = json.Length;
            
            multipartContent.Add(jsonContent, "json_data");

            var response = await client.PostAsync("/empty-response/1", multipartContent);

            Assert.True(response.IsSuccessStatusCode);
            
            var arguments = captureArgumentsEndpointFilter.CapturedArguments;
            Assert.NotNull(arguments);
            Assert.True(arguments.Count == 1);
            
            var myClass = arguments.OfType<MyClass>().Single();
            Assert.Equal(1, myClass.Identifier);
            Assert.Equal(expectedText, myClass.Text);
            Assert.Equal(expectedNumber, myClass.Number);
            
            var complexType = JsonConvert.DeserializeObject<ComplexType>(myClass.AComplexType["json_data"].ToString());
            
            Assert.Equal(expectedComplexTypeSomeText, complexType?.SomeText);
            Assert.Equal(expectedComplexTypeSomeMoreText, complexType?.SomeMoreText);
        }
    }
}