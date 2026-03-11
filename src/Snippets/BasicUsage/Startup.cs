using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.BasicUsage;

static class BasicUsageSnippets
{
    static void Run()
    {
        // begin-snippet: sample-startup
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddViewModelComposition();

        var app = builder.Build();
        app.MapCompositionHandlers();
        app.Run();
        // end-snippet
    }
}
