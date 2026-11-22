using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests
{
    public class When_conventions_read_application_services
    {
        class ConventionDependency
        {
            public string Value => "from application services";
        }

        record ValueFromConvention(string Value);

        class Handler : ICompositionRequestsHandler
        {
            [HttpGet("/application-services/{id}")]
            public Task Handle(HttpRequest request)
            {
                var vm = request.GetComposedResponseModel();
                vm.Value = request.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ValueFromConvention>()?.Value;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Conventions_resolve_application_services_when_the_endpoints_are_built()
        {
            var client = new SelfContainedWebApplicationFactoryWithWebHost<When_conventions_read_application_services>
            (
                configureServices: services =>
                {
                    services.AddSingleton<ConventionDependency>();
                    services.AddViewModelComposition(options =>
                    {
                        options.AssemblyScanner.Disable();
                        options.RegisterCompositionHandler<Handler>();
                    });
                    services.AddRouting();
                },
                configure: app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(builder => builder.MapCompositionHandlers().Add(endpointBuilder =>
                    {
                        var dependency = endpointBuilder.ApplicationServices.GetService<ConventionDependency>();
                        endpointBuilder.Metadata.Add(new ValueFromConvention(dependency?.Value));
                    }));
                }
            ).CreateClient();
            client.DefaultRequestHeaders.Add("Accept-Casing", "casing/pascal");

            var response = await client.GetStringAsync("/application-services/1");

            Assert.Contains("\"Value\":\"from application services\"", response);
        }
    }
}
