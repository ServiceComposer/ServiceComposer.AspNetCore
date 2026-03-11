using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

static class ConfigurationBasedSetupSnippets
{
    static void ShowBasicConfigurationSetup()
    {
        // begin-snippet: scatter-gather-from-configuration
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHttpClient();
        builder.Services.AddScatterGather();

        var app = builder.Build();
        app.MapScatterGather(builder.Configuration.GetSection("ScatterGather"));
        app.Run();
        // end-snippet
    }

    static void ShowConfigurationWithExtraRoute(IConfiguration configuration)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-from-configuration-with-extra-route
        // Routes loaded from appsettings.json (or any IConfiguration source)
        app.MapScatterGather(configuration.GetSection("ScatterGather"));

        // Additional route defined purely in code
        app.MapScatterGather("api/other", new ScatterGatherOptions
        {
            Gatherers = new List<IGatherer>
            {
                new HttpGatherer("OtherSource", "https://other.web.server/api/items")
            }
        });
        // end-snippet

        app.Run();
    }

    static void ShowConfigurationWithCustomization(IConfiguration configuration)
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // begin-snippet: scatter-gather-from-configuration-with-customization
        app.MapScatterGather(
            configuration.GetSection("ScatterGather"),
            customize: (template, options) =>
            {
                if (template == "api/products")
                {
                    // Inject an additional gatherer not present in the configuration file
                    options.Gatherers.Add(new HttpGatherer("Reviews", "https://reviews.web.server/api/reviews"));
                }
            });
        // end-snippet

        app.Run();
    }
}

// begin-snippet: scatter-gather-custom-gatherer-type
class StaticProductDetails(string key) : IGatherer
{
    public string Key { get; } = key;

    public Task<IEnumerable<object>> Gather(HttpContext context)
    {
        var data = (IEnumerable<object>)[new { Value = "InStockItem" }];
        return Task.FromResult(data);
    }
}
// end-snippet

// begin-snippet: scatter-gather-custom-gatherer-type-extra-properties
class GathererWithProperties(string key, string category, int maxItems) : IGatherer
{
    public string Key { get; } = key;

    public Task<IEnumerable<object>> Gather(HttpContext context)
    {
        // use category and maxItems to filter/limit results from a data source
        var data = (IEnumerable<object>)[new { Category = category, MaxItems = maxItems }];
        return Task.FromResult(data);
    }
}
// end-snippet

static class ConfigurationBasedSetupWithCustomGathererSnippets
{
    static void ShowCustomGathererTypeServices()
    {
        // begin-snippet: scatter-gather-from-configuration-with-custom-type-services
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHttpClient();
        builder.Services.AddScatterGather(config =>
        {
            config.AddGathererFactory(
                "StaticProductDetails",
                (section, _) => new StaticProductDetails(section["Key"]));
        });
        // end-snippet
    }

    static void ShowCustomGathererTypeConfigure()
    {
        // begin-snippet: scatter-gather-from-configuration-with-custom-type-configure
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();
        app.MapScatterGather(builder.Configuration.GetSection("ScatterGather"));
        app.Run();
        // end-snippet
    }

    static void ShowCustomGathererTypeExtraProperties()
    {
        // begin-snippet: scatter-gather-from-configuration-with-custom-type-extra-properties
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddHttpClient();
        builder.Services.AddScatterGather(config =>
        {
            config.AddGathererFactory(
                "WithProperties",
                (section, _) => new GathererWithProperties(
                    section["Key"],
                    section["Category"],
                    section.GetValue<int>("MaxItems")));
        });
        // end-snippet
    }
}
