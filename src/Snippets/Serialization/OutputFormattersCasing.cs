using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using ServiceComposer.AspNetCore;

namespace Snippets.Serialization;

static class OutputFormattersCasingSnippets
{
    static void SystemTextJsonCamelCase()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: output-formatters-system-text-json-camel-case
        builder.Services.AddViewModelComposition(options =>
        {
            options.ResponseSerialization.UseOutputFormatters = true;
        });
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Properties are camel cased by default, the dynamic view model's members aren't
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            });
        // end-snippet
    }

    static void SystemTextJsonPascalCase()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: output-formatters-system-text-json-pascal-case
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });
        // end-snippet
    }

    static void NewtonsoftCamelCase()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: output-formatters-newtonsoft-camel-case
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    // Also camel case the dynamic view model's members
                    NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true }
                };
            });
        // end-snippet
    }

    static void NewtonsoftPascalCase()
    {
        var builder = WebApplication.CreateBuilder();

        // begin-snippet: output-formatters-newtonsoft-pascal-case
        builder.Services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
        // end-snippet
    }
}
