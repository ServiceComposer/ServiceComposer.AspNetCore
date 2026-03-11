using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization;

static class SerializationStartupSnippets
{
    static void ShowConfiguration()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: custom-serialization-settings
        builder.Services.AddRouting();
        builder.Services.AddViewModelComposition(options =>
        {
            options.ResponseSerialization.UseCustomJsonSerializerSettings(_ =>
            {
                return new JsonSerializerOptions()
                {
                    // customize options as needed
                };
            });
        });
        // end-snippet
    }
}
