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
    // Regression test: the cached endpoint filter pipeline used to capture
    // the first request's bound arguments and reuse them on every subsequent
    // request. A second call to the same endpoint with a different route
    // value would see the first request's value.
    public class Per_request_arguments_are_not_shared
    {
        [CompositionHandler]
        public class EchoIdHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/echo/{id}")]
            public Task Echo(int id)
            {
                var vm = httpContextAccessor.HttpContext!.Request.GetComposedResponseModel();
                vm.Id = id;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Two_sequential_requests_each_get_their_own_route_value()
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<EchoIdHandler>();
                        options.RegisterCompositionHandler<Generated.Per_request_arguments_are_not_shared_EchoIdHandler_Echo_int_id>();
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

            var first = await client.GetAsync("/echo/1");
            var second = await client.GetAsync("/echo/2");

            Assert.True(first.IsSuccessStatusCode);
            Assert.True(second.IsSuccessStatusCode);

            var firstId = JObject.Parse(await first.Content.ReadAsStringAsync())
                .SelectToken("Id")?.Value<int>();
            var secondId = JObject.Parse(await second.Content.ReadAsStringAsync())
                .SelectToken("Id")?.Value<int>();

            Assert.Equal(1, firstId);
            Assert.Equal(2, secondId);
        }
    }
}
