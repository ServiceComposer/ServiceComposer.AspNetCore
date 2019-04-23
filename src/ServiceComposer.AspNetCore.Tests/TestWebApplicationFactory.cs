using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ServiceComposer.AspNetCore.Tests
{
    public class TestWebApplicationFactory<TEntryPoint> :
        WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<TEntryPoint>();

            return host;
        }
    }
}
