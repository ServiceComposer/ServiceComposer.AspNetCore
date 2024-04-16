using System.Net;
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
    public class When_disabling_write_support
    {
        class TestHandler : ICompositionRequestsHandler
        {
            [HttpPost("/sample")]
            public Task Handle(HttpRequest request)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task It_throws()
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.DisableWriteSupport();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            var stringContent = new StringContent(JsonConvert.SerializeObject(new object()), Encoding.UTF8, MediaTypeNames.Application.Json);

            // Act
            var response = await client.PostAsync("/sample", stringContent);

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}