using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.EndpointFilters
{
    public class Startup
    {
        // begin-snippet: sample-endpoint-filter-registration
        public void Configure(IApplicationBuilder app)
        {
            app.UseEndpoints(builder =>
            {
                builder.MapCompositionHandlers()
                    .AddEndpointFilter(new SampleEndpointFilter());
            });
        }
        // end-snippet
    }
}