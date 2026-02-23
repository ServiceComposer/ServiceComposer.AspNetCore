using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

public class ConfigurationBasedSetupBasic
{
    // begin-snippet: scatter-gather-from-configuration
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHttpClient();
        services.AddScatterGatherer();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.MapScatterGather(configuration.GetSection("ScatterGather"));
        });
    }
    // end-snippet
}

public class ConfigurationBasedSetupWithProgrammaticRoute
{
    // begin-snippet: scatter-gather-from-configuration-with-extra-route
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        app.UseEndpoints(builder =>
        {
            // Routes loaded from appsettings.json (or any IConfiguration source)
            builder.MapScatterGather(configuration.GetSection("ScatterGather"));

            // Additional route defined purely in code
            builder.MapScatterGather("api/other", new ScatterGatherOptions
            {
                Gatherers = new List<IGatherer>
                {
                    new HttpGatherer("OtherSource", "https://other.web.server/api/items")
                }
            });
        });
    }
    // end-snippet
}

public class ConfigurationBasedSetupWithCustomization
{
    // begin-snippet: scatter-gather-from-configuration-with-customization
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        app.UseEndpoints(builder =>
        {
            builder.MapScatterGather(
                configuration.GetSection("ScatterGather"),
                customize: (template, options) =>
                {
                    if (template == "api/products")
                    {
                        // Inject an additional gatherer not present in the configuration file
                        options.Gatherers.Add(new HttpGatherer("Reviews", "https://reviews.web.server/api/reviews"));
                    }
                });
        });
    }
    // end-snippet
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

public class ConfigurationBasedSetupWithCustomGathererType
{
    // begin-snippet: scatter-gather-from-configuration-with-custom-type-services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHttpClient();
        services.AddScatterGatherer(config =>
        {
            config.AddGathererFactory(
                "StaticProductDetails",
                (section, _) => new StaticProductDetails(section["Key"]));
        });
    }
    // end-snippet

    // begin-snippet: scatter-gather-from-configuration-with-custom-type-configure
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.MapScatterGather(configuration.GetSection("ScatterGather"));
        });
    }
    // end-snippet
}

public class ConfigurationBasedSetupWithCustomGathererTypeAndExtraProperties
{
    // begin-snippet: scatter-gather-from-configuration-with-custom-type-extra-properties
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddHttpClient();
        services.AddScatterGatherer(config =>
        {
            config.AddGathererFactory(
                "WithProperties",
                (section, _) => new GathererWithProperties(
                    section["Key"],
                    section["Category"],
                    section.GetValue<int>("MaxItems")));
        });
    }
    // end-snippet
}
