using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceComposer.AspNetCore.Testing;
using ServiceComposer.AspNetCore.Tests.Utils;
using Xunit;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class Get_with_output_formatters
{
    // A plain .NET type that both JSON and XML formatters can serialize
    public class SampleItem
    {
        public string Value { get; set; }
        public string Source { get; set; }
    }

    // An aggregator that knows the concrete item type, enabling XML serialization.
    // Users provide this when they know the element type of their scatter/gather operation.
    class TypedAggregator : IAggregator
    {
        readonly ConcurrentBag<SampleItem> allItems = new();

        public void Add(IEnumerable<object> nodes)
        {
            foreach (var node in nodes)
            {
                allItems.Add((SampleItem)node);
            }
        }

        public Task<object> Aggregate() => Task.FromResult((object)allItems.ToArray());
    }

    class JsonSourceGatherer : Gatherer<SampleItem>
    {
        readonly HttpClient _client;
        public JsonSourceGatherer(HttpClient client) : base("JsonSource") => _client = client;

        public override async Task<IEnumerable<SampleItem>> Gather(HttpContext context)
        {
            var response = await _client.GetAsync("/items/json-source");
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SampleItem[]>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? Array.Empty<SampleItem>();
        }
    }

    class XmlSourceGatherer : Gatherer<SampleItem>
    {
        readonly HttpClient _client;
        public XmlSourceGatherer(HttpClient client) : base("XmlSource") => _client = client;

        public override async Task<IEnumerable<SampleItem>> Gather(HttpContext context)
        {
            var response = await _client.GetAsync("/items/xml-source");
            var xml = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xml);
            var items = new List<SampleItem>();
            foreach (var el in doc.Descendants("SampleItem"))
            {
                items.Add(new SampleItem
                {
                    Value = el.Element("Value")?.Value,
                    Source = el.Element("Source")?.Value
                });
            }
            return items;
        }
    }

    HttpClient BuildDownstreamClient()
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers();
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapGet("/items/json-source", () =>
                        new[] { new SampleItem { Value = "FromJson", Source = "json" } });

                    // XML endpoint returns raw XML string (simulates an XML-producing service)
                    builder.MapGet("/items/xml-source", () =>
                        "<ArrayOfSampleItem><SampleItem><Value>FromXml</Value><Source>xml</Source></SampleItem></ArrayOfSampleItem>");
                });
            }
        ).CreateClient();
    }

    HttpClient BuildComposerClient(
        HttpClient downstreamClient,
        bool useOutputFormatters,
        Type customAggregator = null)
    {
        return new SelfContainedWebApplicationFactoryWithWebHost<Dummy>
        (
            configureServices: services =>
            {
                services.AddRouting();
                services.AddControllers()
                    .AddXmlSerializerFormatters();
                if (customAggregator != null)
                {
                    services.AddTransient(customAggregator);
                }
                services.Replace(new ServiceDescriptor(typeof(IHttpClientFactory),
                    new DelegateHttpClientFactory(_ => downstreamClient)));
            },
            configure: app =>
            {
                app.UseRouting();
                app.UseEndpoints(builder =>
                {
                    builder.MapScatterGather(template: "/items", new ScatterGatherOptions
                    {
                        UseOutputFormatters = useOutputFormatters,
                        CustomAggregator = customAggregator,
                        Gatherers = new List<IGatherer>
                        {
                            new JsonSourceGatherer(downstreamClient),
                            new XmlSourceGatherer(downstreamClient)
                        }
                    });
                });
            }
        ).CreateClient();
    }

    [Fact]
    public async Task Returns_json_when_accept_is_json_and_output_formatters_enabled()
    {
        var downstream = BuildDownstreamClient();
        var client = BuildComposerClient(downstream, useOutputFormatters: true);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync("/items");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Returns_xml_when_accept_is_xml_and_typed_aggregator_provided()
    {
        // When one gatherer fetches JSON and another fetches XML, and the client expects XML,
        // both gatherers normalize their data to typed C# objects (SampleItem).
        // A typed custom aggregator is used so the XML serializer knows the concrete element type.
        var downstream = BuildDownstreamClient();
        var client = BuildComposerClient(downstream, useOutputFormatters: true, customAggregator: typeof(TypedAggregator));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        var response = await client.GetAsync("/items");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("application/xml", response.Content.Headers.ContentType?.MediaType);

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);
        var items = doc.Descendants("SampleItem").ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task UseOutputFormatters_false_always_returns_json()
    {
        var downstream = BuildDownstreamClient();
        // When UseOutputFormatters is false, output is always JSON regardless of Accept header
        var client = BuildComposerClient(downstream, useOutputFormatters: false);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        var response = await client.GetAsync("/items");

        // The DefaultAggregator produces a JsonArray serialized to JSON
        Assert.True(response.IsSuccessStatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }
}

