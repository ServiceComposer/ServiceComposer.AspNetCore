using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.CompositionHandlers
{
    public class Get_with_2_handlers_output_formatter_file_stream_result
    {
        static readonly string expected_string_content = Guid.NewGuid().ToString();

        [CompositionHandler]
        public class TestGetStringCompositionHandler(IHttpContextAccessor httpContextAccessor)
        {
            [HttpGet("/sample/using-body-stream")]
            public Task Handle()
            {
                var buffer = Encoding.UTF8.GetBytes(expected_string_content);
                httpContextAccessor.HttpContext.Request.SetActionResult(new FileStreamResult
                (
                    new MemoryStream(buffer),
                    "text/plain"
                ));

                return Task.CompletedTask;
            }
        }

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
                        options.RegisterCompositionHandler<TestGetStringCompositionHandler>();
                        options.RegisterCompositionHandler<Generated.Get_with_2_handlers_output_formatter_file_stream_result_TestGetStringCompositionHandler_Handle>();
                        options.ResponseSerialization.UseOutputFormatters = true;
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
            var response = await client.GetAsync("/sample/using-body-stream");

            // Assert
            Assert.True(response.IsSuccessStatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected_string_content, responseString);
        }
    }
}