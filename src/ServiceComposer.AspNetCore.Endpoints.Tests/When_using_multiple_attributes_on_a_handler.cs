using System.Dynamic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Endpoints.Tests
{
    public class When_using_multiple_attributes_on_a_handler
    {
        public class MultipleAttributesOfDifferentTypesHandler : ICompositionRequestsHandler
        {
            [HttpPost("/multiple/attributes")]
            [HttpGet("/multiple/attributes/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.RequestPath = request.Path;

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task If_attributes_are_of_different_types_handler_should_be_invoked_for_all_routes()
        {
            // Arrange
            var client =
                new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterCompositionHandler<MultipleAttributesOfDifferentTypesHandler>();
                        });
                        services.AddControllers();
                        services.AddRouting();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder =>
                        {
                            builder.MapCompositionHandlers();
                            builder.MapControllers();
                        });
                    }
                ).CreateClient();

            var json = "{}";
            var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
            stringContent.Headers.ContentLength = json.Length;

            // Act
            var postResponse = await client.PostAsync("/multiple/attributes", stringContent);
            var getResponse = await client.GetAsync("/multiple/attributes/2");

            // Assert
            //Assert.True(composedResponse.IsSuccessStatusCode);
        }
        
        public class MultipleGetAttributesDifferentTemplatesHandler : ICompositionRequestsHandler
        {
            [HttpGet("/multiple/attributes")]
            [HttpGet("/multiple/attributes/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.RequestPath = request.Path;

                return Task.CompletedTask;
            }
        }
        
        [Fact]
        public async Task If_attributes_are_of_the_same_type_handler_should_be_invoked_for_all_routes()
        {
            // Arrange
            var client =
                new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterCompositionHandler<MultipleGetAttributesDifferentTemplatesHandler>();
                        });
                        services.AddControllers();
                        services.AddRouting();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder =>
                        {
                            builder.MapCompositionHandlers();
                            builder.MapControllers();
                        });
                    }
                ).CreateClient();

            // Act
            var composedResponse1 = await client.GetAsync("/multiple/attributes");
            var composedResponse2 = await client.GetAsync("/multiple/attributes/2");

            // Assert
            Assert.True(composedResponse1.IsSuccessStatusCode);
            Assert.True(composedResponse2.IsSuccessStatusCode);
        }
        
        class InvocationCountViewModel
        {
            private int invocationCount = 0;
            public int InvocationCount => invocationCount;

            public void IncrementInvocationCount()
            {
                Interlocked.Increment(ref invocationCount);
            }
        }
        
        class InvocationCountViewModelFactory : IViewModelFactory
        {
            public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
            {
                return new InvocationCountViewModel();
            }
        }
        
        public class MultipleGetAttributesSameTemplateHandler : ICompositionRequestsHandler
        {
            [HttpGet("/multiple/attributes")]
            [HttpGet("/multiple/attributes")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel<InvocationCountViewModel>();
                vm.IncrementInvocationCount();

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task If_attributes_are_of_the_same_type_and_same_template_handler_should_be_invoked_multiple_times()
        {
            // Arrange
            var client =
                new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
                (
                    configureServices: services =>
                    {
                        services.AddViewModelComposition(options =>
                        {
                            options.AssemblyScanner.Disable();
                            options.RegisterGlobalViewModelFactory<InvocationCountViewModelFactory>();
                            options.RegisterCompositionHandler<MultipleGetAttributesSameTemplateHandler>();
                        });
                        services.AddControllers();
                        services.AddRouting();
                    },
                    configure: app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(builder =>
                        {
                            builder.MapCompositionHandlers();
                            builder.MapControllers();
                        });
                    }
                ).CreateClient();

            // Act
            var composedResponse = await client.GetAsync("/multiple/attributes");

            // Assert
            Assert.True(composedResponse.IsSuccessStatusCode);
            
            var responseString = await composedResponse.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);
            var invocationCount = responseObj?.GetValue("invocationCount")?.Value<int>();
            
            Assert.Equal(2, invocationCount);
        }
    }
}