using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
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
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        app.UseRouting();
        app.UseEndpoints(builder =>
        {
            builder.MapScatterGatherFromConfiguration(configuration.GetSection("ScatterGather"));
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
            builder.MapScatterGatherFromConfiguration(configuration.GetSection("ScatterGather"));

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
            builder.MapScatterGatherFromConfiguration(
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
