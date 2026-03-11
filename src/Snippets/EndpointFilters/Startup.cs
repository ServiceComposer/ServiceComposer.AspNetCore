using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.EndpointFilters;

static class EndpointFiltersSnippets
{
    static void ShowRegistration()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: sample-endpoint-filter-registration
        app.MapCompositionHandlers()
            .AddEndpointFilter(new SampleEndpointFilter());
        // end-snippet

        app.Run();
    }
}
