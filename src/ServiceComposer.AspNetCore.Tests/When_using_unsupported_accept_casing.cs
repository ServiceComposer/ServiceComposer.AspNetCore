using System;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_using_unsupported_accept_casing
    {
        class TestGetHandler : ICompositionRequestsHandler
        {
            [HttpGet("/sample/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.AString = "sample";
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Logs_warning_and_throws_for_unsupported_casing_value()
        {
            // Arrange
            var loggerFactory = TestLoggerFactory.Create(options => options.FilterByTypeName<CompositionEndpointBuilder>());

            var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
            (
                configureServices: services =>
                {
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<TestGetHandler>();
                    });
                    services.AddRouting();
                    services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers());
                }
            ).CreateClient();

            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/unknown");

            // Act
            await Assert.ThrowsAsync<NotSupportedException>(() => client.GetAsync("/sample/1"));

            // Assert
            var log = Assert.Single(loggerFactory.Sink.LogEntries, l => l.LogLevel == LogLevel.Warning);
            Assert.Equal("Unsupported Accept-Casing header value {RequestedCasing}. Supported values are 'casing/pascal' and 'casing/camel'.", log.OriginalFormat);
            var requestedCasing = Assert.Single(log.Properties, p => p.Key == "RequestedCasing").Value;
            Assert.Equal("casing/unknown", requestedCasing);
        }
    }
}
