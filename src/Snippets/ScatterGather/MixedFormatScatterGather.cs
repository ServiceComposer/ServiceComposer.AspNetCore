using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ServiceComposer.AspNetCore;

namespace Snippets.ScatterGather;

// begin-snippet: scatter-gather-mixed-format-gatherers
public class SampleItem
{
    public string Value { get; set; }
    public string Source { get; set; }
}

public class JsonSourceGatherer() : Gatherer<SampleItem>("JsonSource")
{
    public override Task<IEnumerable<SampleItem>> Gather(HttpContext context)
    {
        // fetch JSON from downstream service and deserialize to SampleItem[]
        throw new NotImplementedException();
    }
}

public class XmlSourceGatherer() : Gatherer<SampleItem>("XmlSource")
{
    public override Task<IEnumerable<SampleItem>> Gather(HttpContext context)
    {
        // fetch XML from downstream service and parse to List<SampleItem>
        throw new NotImplementedException();
    }
}

public class TypedAggregator : IAggregator
{
    readonly ConcurrentBag<SampleItem> allItems = new();

    public void Add(IEnumerable<object> nodes)
    {
        foreach (var node in nodes)
        {
            allItems.Add((SampleItem)node);
        }
    }

    public Task<object> Aggregate() => Task.FromResult<object>(allItems.ToArray());
}
// end-snippet

static class MixedFormatStartupSnippets
{
    static void ShowMixedFormatSetup()
    {
        // begin-snippet: scatter-gather-mixed-format-startup
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddXmlSerializerFormatters();
        builder.Services.AddTransient<TypedAggregator>();

        var app = builder.Build();
        app.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
        {
            UseOutputFormatters = true,
            CustomAggregator = typeof(TypedAggregator),
            Gatherers = new List<IGatherer>
            {
                new JsonSourceGatherer(),
                new XmlSourceGatherer()
            }
        });
        app.Run();
        // end-snippet
    }
}
