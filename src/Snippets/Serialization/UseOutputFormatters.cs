using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization;

static class UseOutputFormattersSnippets
{
    static void ShowOutputFormatters()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: use-output-formatters
        builder.Services.AddViewModelComposition(options =>
        {
            options.ResponseSerialization.UseOutputFormatters = true;
        });
        builder.Services.AddControllers();
        // end-snippet
    }

    static void ShowNewtonsoftOutputFormatters()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: use-newtonsoft-output-formatters
        builder.Services.AddViewModelComposition(options =>
        {
            options.ResponseSerialization.UseOutputFormatters = true;
        });
        builder.Services.AddControllers()
            .AddNewtonsoftJson();
        // end-snippet
    }
}
