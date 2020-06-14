using System;
using System.Dynamic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.When_using_endpoints
{
    public class Post_with_2_handlers
    {
        class TestIntegerHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var requestContent = Encoding.UTF8.GetString(buffer);

                request.Body.Position = 0;

                var content = JObject.Parse(requestContent);
                
                var vm = request.GetResponseModel();
                vm.ANumber = content?.SelectToken("ANumber")?.Value<int>();
            }
        }

        class TestStrinHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample/{id}")]
            public async Task Handle(HttpRequest request)
            {
                var buffer = new byte[Convert.ToInt32(request.ContentLength)];
                await request.Body.ReadAsync(buffer, 0, buffer.Length);
                var requestContent = Encoding.UTF8.GetString(buffer);

                request.Body.Position = 0;

                var content = JObject.Parse(requestContent);
                
                var vm = request.GetResponseModel();
                vm.AString = content?.SelectToken("AString")?.Value<int>();
            }
        }

        [Fact]
        public async Task Returns_expected_response()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_a_matching_handler_is_found>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestStrinHandler>();
                        options.RegisterCompositionHandler<TestIntegerHandler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");

            dynamic model = new ExpandoObject();
            model.AString = "this is a string value";
            model.ANumber = 32;

            var stringContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, 
                MediaTypeNames.Application.Json);
            // Act
            var response = await client.PostAsync("/sample/1", stringContent);

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseObj = JObject.Parse(responseString);

            Assert.Equal((string)model.AString, responseObj?.SelectToken("AString")?.Value<string>());
            Assert.Equal((int)model.ANumber, responseObj?.SelectToken("ANumber")?.Value<int>());
        }
    }
}