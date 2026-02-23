using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Testing;
using ServiceComposer.AspNetCore.Tests.Utils;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_downstream_returns_error
{
    static HttpClient BuildDownstreamClientReturning(HttpStatusCode statusCode)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/upstream/source", (Microsoft.AspNetCore.Http.HttpContext ctx) =>
                    {
                        ctx.Response.StatusCode = (int)statusCode;
                    });
                });
            }
        ).CreateClient();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Non_success_status_from_downstream_propagates_as_exception(HttpStatusCode statusCode)
    {
        // Arrange
        var downstreamClient = BuildDownstreamClientReturning(statusCode);

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => downstreamClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/items", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new HttpGatherer("Source", "/upstream/source")
                        }
                    });
                });
            }
        ).CreateClient();

        // Act & Assert — EnsureSuccessStatusCode() in HttpGatherer.Gather() throws
        // HttpRequestException for any non-2xx response; it propagates out of the endpoint.
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("/items"));
    }
}
