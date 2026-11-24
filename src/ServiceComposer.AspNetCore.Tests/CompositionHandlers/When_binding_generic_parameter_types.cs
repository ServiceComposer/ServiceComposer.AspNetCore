using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
    public class GenericBodyItem
    {
        public int Quantity { get; set; }
    }

    public class When_binding_generic_parameter_types
    {
        [CompositionHandler]
        public class TestCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpPost("/generic-parameters/{id}")]
            public Task Post(int id, [FromBody] List<GenericBodyItem> items, [FromQuery] int[] tags)
            {
                var vm = httpContextAccessor.HttpContext!.Request.GetComposedResponseModel();
                vm.Id = id;
                vm.TotalQuantity = items.Sum(i => i.Quantity);
                vm.TagCount = tags.Length;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Contract_less_handlers_bind_generic_types_and_arrays()
        {
            // Arrange
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.ResponseSerialization.DefaultResponseCasing = ResponseCasing.PascalCase;
                        options.RegisterCompositionHandler<TestCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.When_binding_generic_parameter_types_TestCompositionHandler_Post_int_id_System_Collections_Generic_List_ServiceComposer_AspNetCore_Tests_CompositionHandlers_GenericBodyItem__items_int___tags>();
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
            var response = await client.PostAsJsonAsync("/generic-parameters/42?tags=1&tags=2&tags=3",
                new[] { new GenericBodyItem { Quantity = 2 }, new GenericBodyItem { Quantity = 3 } });

            // Assert
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(42, responseBody.Value<int>("Id"));
            Assert.Equal(5, responseBody.Value<int>("TotalQuantity"));
            Assert.Equal(3, responseBody.Value<int>("TagCount"));
        }
    }
}
