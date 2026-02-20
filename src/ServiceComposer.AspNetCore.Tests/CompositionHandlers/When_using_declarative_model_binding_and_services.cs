using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlers
{
    public interface ITestService
    {
        string GetValue();
    }

    public class When_using_declarative_model_binding_and_services
    {
        class TestService : ITestService
        {
            public string GetValue() => "value-from-service";
        }

        [CompositionHandler]
        public class TestCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/sample-services/{id}")]
            public Task Get(int id, [FromServices] ITestService testService)
            {
                var vm = httpContextAccessor.HttpContext!.Request.GetComposedResponseModel();
                vm.Id = id;
                vm.ServiceValue = testService.GetValue();
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Should_resolve_service_from_di_container()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddSingleton<ITestService, TestService>();
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.RegisterCompositionHandler<TestCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_using_declarative_model_binding_and_services_TestCompositionHandler_Get_int_id_ServiceComposer_AspNetCore_Tests_CompositionHandlers_ITestService_testService>();
                    });
                    services.AddRouting();
                    services.AddControllers();
                    services.AddHttpContextAccessor();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample-services/42");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var contentString = await response.Content.ReadAsStringAsync();
            dynamic responseBody = JObject.Parse(contentString);
            Assert.Equal(42, (int)responseBody.Id);
            Assert.Equal("value-from-service", (string)responseBody.ServiceValue);
        }
    }
}
