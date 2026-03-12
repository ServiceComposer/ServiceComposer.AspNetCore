using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Snippets.OpenTelemetry;

static class OpenTelemetrySetupSnippets
{
    static void ViewModelCompositionSetup()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: open-telemetry-view-model-composition-setup
        builder.Services.AddOpenTelemetry()
            .WithTracing(b => b
                .AddSource("ServiceComposer.AspNetCore.ViewModelComposition"));
        // end-snippet
    }

    static void ScatterGatherSetup()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: open-telemetry-scatter-gather-setup
        builder.Services.AddOpenTelemetry()
            .WithTracing(b => b
                .AddSource("ServiceComposer.AspNetCore.ScatterGather"));
        // end-snippet
    }

    static void CombinedSetup()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: open-telemetry-combined-setup
        builder.Services.AddOpenTelemetry()
            .WithTracing(b => b
                .AddSource("ServiceComposer.AspNetCore.ViewModelComposition")
                .AddSource("ServiceComposer.AspNetCore.ScatterGather"));
        // end-snippet
    }
}
