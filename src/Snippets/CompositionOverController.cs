using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets;

static class CompositionOverControllersSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: enable-composition-over-controllers
        builder.Services.AddViewModelComposition(options =>
        {
            options.EnableCompositionOverControllers();
        });
        // end-snippet
    }

    static void ShowEndpointFilters(WebApplication app)
    {
        // begin-snippet: composition-over-controllers-endpoint-filters
        app.MapControllers()
            // Wraps both the controller action and the composition handlers
            .AddEndpointFilter(async (context, next) =>
            {
                var result = await next(context);
                // result is the action result, including the one
                // set by composition handlers with SetActionResult
                return result;
            })
            // Must be the last endpoint filter added
            .WithCompositionOverControllers();
        // end-snippet
    }
}
