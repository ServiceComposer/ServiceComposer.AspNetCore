using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MELT;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_scatter_gather_logs
{
    class FixedGatherer(string key, object[] items) : IGatherer
    {
        public string Key { get; } = key;

        public Task<IEnumerable<object>> Gather(HttpContext context)
            => Task.FromResult<IEnumerable<object>>(items);
    }

    [Fact]
    public async Task Logs_debug_on_execution_with_template_and_gatherer_count()
    {
        // Arrange
        var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(template: "/samples", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer>
                        {
                            new FixedGatherer("Source1", [new { Value = "A" }]),
                            new FixedGatherer("Source2", [new { Value = "B" }])
                        }
                    });
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/samples");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        // Some ASP.NET Core internal log entries hold lazy references to the now-disposed
        // HttpContext, so reading OriginalFormat on them throws ObjectDisposedException.
        // The try/catch predicate skips those and finds only our specific entry.
        var log = loggerFactory.Sink.LogEntries
            .Single(l =>
            {
                try { return l.OriginalFormat == "Executing scatter-gather for {Template} with {GathererCount} gatherer(s)."; }
                catch { return false; }
            });
        Assert.Equal(LogLevel.Debug, log.LogLevel);
        Assert.Equal("/samples", log.Properties.Single(p => p.Key == "Template").Value);
        Assert.Equal(2, log.Properties.Single(p => p.Key == "GathererCount").Value);
    }

    [Fact]
    public void Logs_debug_when_configuration_has_no_route_sections()
    {
        // Arrange
        var loggerFactory = TestLoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug));

        var emptyConfig = new ConfigurationBuilder().Build();

        _ = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddScatterGather();
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(loggerFactory));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder => builder.MapScatterGather(emptyConfig));
            }
        ).CreateClient();

        // Assert — log emitted at startup, no HTTP request needed
        var log = loggerFactory.Sink.LogEntries
            .Single(l => l.OriginalFormat == "No scatter-gather route sections found in configuration. No endpoints will be registered.");
        Assert.Equal(LogLevel.Warning, log.LogLevel);
    }
}
