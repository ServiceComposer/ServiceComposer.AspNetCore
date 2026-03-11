using Microsoft.AspNetCore.Builder;
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
}
