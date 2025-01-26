using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using ServiceComposer_AspNetCore_Tests_CompositionHandlersTests_CompositionHandlers_Generated;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlersTests.CompositionHandlers
{
    public class TestGetIntegerCompositionHandler(IHttpContextAccessor httpContextAccessor)
    {
        [HttpGet("/sample/{id}")]
        public Task SomeMethod(int id)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var vm = request.GetComposedResponseModel();
            vm.ANumber = id;
                
            return Task.CompletedTask;
        }
    }
    
    public class TestGetStringCompositionHandler(IHttpContextAccessor httpContextAccessor)
    {
        [HttpGet("/sample/{id}")]
        public Task AnotherMethod()
        {
            var vm = httpContextAccessor.HttpContext.Request.GetComposedResponseModel();
            vm.AString = "sample";
            return Task.CompletedTask;
        }
    }
}

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlersTests
{
    public class Get_with_2_handlers
    {
        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterContractLessCompositionHandler(typeof(CompositionHandlers.TestGetIntegerCompositionHandler));
                        options.RegisterContractLessCompositionHandler(typeof(CompositionHandlers.TestGetStringCompositionHandler));
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler_AnotherMethod>();
                        options.RegisterCompositionHandler<TestGetIntegerCompositionHandler_SomeMethod_int_id>();
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

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");
            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }

        [Fact]
        public async Task Returns_expected_response_using_output_formatters()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        //options.RegisterCompositionHandler<TestGetStringHandler>();
                        //options.RegisterCompositionHandler<TestGetIntegerHandler>();
                        options.ResponseSerialization.UseOutputFormatters = true;
                    });
                    services.AddRouting();
                    services.AddControllers()
                        .AddNewtonsoftJson();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            // Act
            var response = await client.GetAsync("/sample/1");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal("sample", responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal(1, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }
}