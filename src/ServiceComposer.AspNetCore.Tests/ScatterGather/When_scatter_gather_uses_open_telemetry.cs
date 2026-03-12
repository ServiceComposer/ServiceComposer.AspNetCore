using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore.Testing;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class When_scatter_gather_uses_open_telemetry
{
    static ActivityListener CreateActivityListener(ConcurrentBag<Activity> capturedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ServiceComposer.AspNetCore.ScatterGather",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    // Each test uses a unique gatherer key to avoid Assert.Single predicate collisions
    // from parallel tests sharing the same process-global ActivityListener source.

    class OTelTestGatherer : IGatherer
    {
        public string Key => "OTelTestGatherer";

        public Task<IEnumerable<object>> Gather(HttpContext context) =>
            Task.FromResult<IEnumerable<object>>(new object[] { new { Value = "sample" } });
    }

    [Fact]
    public async Task Gatherer_span_is_created_with_correct_name_and_tag()
    {
        // Arrange
        var capturedActivities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(capturedActivities);

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/samples", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer> { new OTelTestGatherer() }
                    });
                });
            }
        ).CreateClient();

        // Act
        var response = await client.GetAsync("/samples");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var gathererActivity = Assert.Single(capturedActivities, a => a.DisplayName == "scatter-gather.gatherer OTelTestGatherer");
        Assert.Equal("OTelTestGatherer", gathererActivity.GetTagItem("scatter-gather.gatherer.key"));
    }

    class OTelFailingGatherer : IGatherer
    {
        public string Key => "OTelFailingGatherer";

        public Task<IEnumerable<object>> Gather(HttpContext context) =>
            throw new InvalidOperationException("Gatherer failed");
    }

    [Fact]
    public async Task Gatherer_span_has_error_status_when_gatherer_throws()
    {
        // Arrange
        var capturedActivities = new ConcurrentBag<Activity>();
        using var listener = CreateActivityListener(capturedActivities);

        var client = new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services => services.AddRouting(),
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather("/samples", new ScatterGatherOptions
                    {
                        Gatherers = new List<IGatherer> { new OTelFailingGatherer() }
                    });
                });
            }
        ).CreateClient();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("/samples"));

        // Assert
        var gathererActivity = Assert.Single(capturedActivities, a => a.DisplayName == "scatter-gather.gatherer OTelFailingGatherer");
        Assert.Equal(ActivityStatusCode.Error, gathererActivity.Status);
        Assert.Equal("Gatherer failed", gathererActivity.StatusDescription);
    }
}
