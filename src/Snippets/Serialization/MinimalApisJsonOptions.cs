using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization;

static class MinimalApisJsonOptionsSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: minimal-apis-json-options
        builder.Services.AddViewModelComposition();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        // end-snippet
    }
}
