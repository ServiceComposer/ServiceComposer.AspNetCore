# Scatter/Gather

ServiceComposer natively supports scatter/gather scenarios. Scatter/gather is supported through a fanout approach. Given an incoming HTTP request, ServiceComposer will issue as many downstream HTTP requests to fetch data from downstream endpoints. Once all data has been retrieved, they are composed and returned to the original upstream caller.

The following configuration configures a scatter/gather endpoint:

<!-- snippet: scatter-gather-basic-usage -->
<a id='snippet-scatter-gather-basic-usage'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseRouting();
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
            new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/Startup.cs#L10-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-basic-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The above configuration snippet configures ServiceComposer to handle HTTP requests matching the template. Each time a matching request is dealt with, ServiceComposer invokes each configured gatherer and merges responses from each one into a response returned to the original issuer.

The `Key` and `Destination` properties are mandatory. The key uniquely identifies each gatherer in the context of a specific request. The destination is the downstream URL of the endpoint to invoke to retrieve data.

## Customizing downstream URLs

If the incoming request contains a query string, the query string and its values are automatically appended to downstream URLs as is. It is possible to override that behavior by setting the `DestinationUrlMapper` delegate as presented in the following snippet:

<!-- snippet: scatter-gather-customizing-downstream-urls -->
<a id='snippet-scatter-gather-customizing-downstream-urls'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
            {
                DestinationUrlMapper = (request, destination) => destination.Replace(
                    "{this-is-contextual}",
                    request.Query["this-is-contextual"])
            }
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/CustomizingDownstreamURLs.cs#L10-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-customizing-downstream-urls' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same approach can be used to customize the downstream URL before invocation.

> **Note:** The default `DefaultDestinationUrlMapper` appends the incoming query string by concatenating `request.QueryString` (which includes the leading `?`). If `destinationUrl` already contains a query string, this will produce a malformed URL (e.g. `…?existing=1?new=2`). In that case, replace the default mapper with one that uses `&` to append additional parameters.

## Forwarding headers

By default, `HttpGatherer` forwards all incoming request headers to the downstream destination. This behavior is controlled by the `ForwardHeaders` property (default: `true`) and can be customized using the `HeadersMapper` delegate, following the same pattern as `DefaultDestinationUrlMapper`/`DestinationUrlMapper`.

> **Security note:** The default `DefaultHeadersMapper` forwards **all** headers verbatim, including sensitive ones such as `Authorization` and `Cookie`. If the downstream service should receive a different credential (e.g. a service-to-service token) or no credential at all, replace `HeadersMapper` to filter or substitute those headers before the request is dispatched.

### Disabling header forwarding

To prevent any headers from being forwarded, set `ForwardHeaders = false`:

<!-- snippet: scatter-gather-disable-header-forwarding -->
<a id='snippet-scatter-gather-disable-header-forwarding'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
            {
                ForwardHeaders = false
            }
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/ForwardingHeaders.cs#L12-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-disable-header-forwarding' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Filtering headers

To selectively forward headers, replace the `HeadersMapper` delegate. The default implementation (`DefaultHeadersMapper`) copies all incoming request headers. Assign a custom delegate to filter, modify, add or remove headers before the downstream request is dispatched:

<!-- snippet: scatter-gather-filter-headers -->
<a id='snippet-scatter-gather-filter-headers'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
            {
                HeadersMapper = (incomingRequest, outgoingMessage) =>
                {
                    foreach (var header in incomingRequest.Headers)
                    {
                        if (header.Key.Equals("x-do-not-forward", StringComparison.OrdinalIgnoreCase))
                            continue;
                        outgoingMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                    }
                }
            }
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/ForwardingHeaders.cs#L31-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-filter-headers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Adding headers

To inject additional headers alongside the forwarded ones:

<!-- snippet: scatter-gather-add-headers -->
<a id='snippet-scatter-gather-add-headers'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
            {
                HeadersMapper = (incomingRequest, outgoingMessage) =>
                {
                    HttpGatherer.DefaultHeadersMapper(incomingRequest, outgoingMessage);
                    outgoingMessage.Headers.TryAddWithoutValidation("x-custom-header", "custom-value");
                }
            }
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/ForwardingHeaders.cs#L58-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-add-headers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Data format

By default, `HttpGatherer` assumes that the downstream endpoint result can be converted into a `JsonArray`. Custom gatherers implementing `IGatherer` can return any object type; the default aggregator will serialize non-JSON values automatically.

### Content negotiation and output formatters

Scatter/gather endpoints can participate in ASP.NET Core's MVC content negotiation (JSON, XML, etc.) by setting `UseOutputFormatters = true` in `ScatterGatherOptions`. When enabled, the response format is determined by the client's `Accept` header instead of always producing JSON.

<!-- snippet: scatter-gather-use-output-formatters -->
<a id='snippet-scatter-gather-use-output-formatters'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        UseOutputFormatters = true,
        Gatherers = new List<IGatherer>
        {
            new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
            new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/UseOutputFormatters.cs#L10-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-use-output-formatters' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To use output formatters, MVC services must be registered (e.g., `services.AddControllers()`).

#### Mixed-format scenario: one JSON source, one XML source, XML response expected

Consider a scenario where one gatherer fetches a JSON response and another fetches XML, and the
original request expects an XML response:

1. Both gatherers normalize downstream data to the same typed C# model.
2. A typed custom aggregator collects the objects and returns a concrete `SampleItem[]` so that XML
   serializers (which need to know the element type at compile time) can serialize the result.
3. `UseOutputFormatters = true` lets ASP.NET Core pick the right formatter based on the `Accept` header.

The gatherers, model, and aggregator:

<!-- snippet: scatter-gather-mixed-format-gatherers -->
<a id='snippet-scatter-gather-mixed-format-gatherers'></a>
```cs
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
```
<sup><a href='/src/Snippets/ScatterGather/MixedFormatScatterGather.cs#L13-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-mixed-format-gatherers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Register the aggregator and XML formatter, then configure the endpoint:

<!-- snippet: scatter-gather-mixed-format-startup -->
<a id='snippet-scatter-gather-mixed-format-startup'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers().AddXmlSerializerFormatters();
    services.AddTransient<TypedAggregator>();
}

public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        UseOutputFormatters = true,
        CustomAggregator = typeof(TypedAggregator),
        Gatherers = new List<IGatherer>
        {
            new JsonSourceGatherer(),
            new XmlSourceGatherer()
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/MixedFormatScatterGather.cs#L56-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-mixed-format-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A client sending `Accept: application/xml` now receives XML; a client sending `Accept: application/json` receives JSON — with the same gatherers and aggregator.

### Transforming returned data

If there is a need to transform downstream data to respect the expected format, it's possible to create a custom gatherer and override the `TransformResponse` method:

<!-- snippet: scatter-gather-transform-response -->
<a id='snippet-scatter-gather-transform-response'></a>
```cs
public class CustomHttpGatherer : HttpGatherer
{
    public CustomHttpGatherer(string key, string destination) : base(key, destination) { }

    protected override Task<IEnumerable<JsonNode>> TransformResponse(HttpResponseMessage responseMessage)
    {
        // retrieve the response as a string from the HttpResponseMessage
        // and parse it as a JsonNode enumerable.
        return base.TransformResponse(responseMessage);
    }
}
```
<sup><a href='/src/Snippets/ScatterGather/TransformResponse.cs#L12-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-transform-response' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Taking control of the downstream invocation process

If transforming returned data is not enough, it's possible to take full control over the downstream service invocation process by overriding the `Gather` method:

<!-- snippet: scatter-gather-gather-override -->
<a id='snippet-scatter-gather-gather-override'></a>
```cs
public class CustomHttpGatherer(string key, string destination) : HttpGatherer(key, destination)
{
    public override Task<IEnumerable<JsonNode>> Gather(HttpContext context)
    {
        return base.Gather(context);
    }
}
```
<sup><a href='/src/Snippets/ScatterGather/GatherMethodOverride.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-gather-override' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Custom gatherers

It is possible to implement fully custom gatherers by implementing the `IGatherer` interface directly. This allows non-HTTP data sources, in-memory data, or any other data retrieval mechanism:

<!-- snippet: scatter-gather-custom-gatherer -->
<a id='snippet-scatter-gather-custom-gatherer'></a>
```cs
class CustomGatherer : IGatherer
{
    public string Key { get; } = "CustomGatherer";

    public Task<IEnumerable<object>> Gather(HttpContext context)
    {
        var data = (IEnumerable<object>)[new { Value = "ACustomSample" }];
        return Task.FromResult(data);
    }
}
```
<sup><a href='/src/Snippets/ScatterGather/CustomGatherer.cs#L8-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-custom-gatherer' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Configuration-based setup

Routes and their gatherers can be defined in an external configuration source such as `appsettings.json`, environment variables, or any other `IConfiguration`-compatible provider. This allows changes to be made without recompiling the application.

### JSON configuration shape

```json
{
  "ScatterGather": [
    {
      "Template": "api/products",
      "Gatherers": [
        { "Key": "AProductsSource", "DestinationUrl": "https://one.web.server/api/a-source" },
        { "Key": "AnotherProductSource", "DestinationUrl": "https://two.web.server/api/another-source" }
      ]
    }
  ]
}
```

Each entry in the array supports the same options available when calling `MapScatterGather` in code, including `UseOutputFormatters`.

### Mapping from configuration

Register scatter/gather services and call `MapScatterGather` (the `IConfiguration` overload), passing the configuration section that contains the route list:

<!-- snippet: scatter-gather-from-configuration -->
<a id='snippet-scatter-gather-from-configuration'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddHttpClient();
    services.AddScatterGather();
}

public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
{
    app.UseRouting();
    app.UseEndpoints(builder =>
    {
        builder.MapScatterGather(configuration.GetSection("ScatterGather"));
    });
}
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L14-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Adding extra routes alongside configuration-defined routes

Calling `MapScatterGather` (the `IConfiguration` overload) and `MapScatterGather` (the template overload) in the same `UseEndpoints` block is fully supported. Routes defined in code are registered in addition to those loaded from configuration:

<!-- snippet: scatter-gather-from-configuration-with-extra-route -->
<a id='snippet-scatter-gather-from-configuration-with-extra-route'></a>
```cs
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
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L35-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration-with-extra-route' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Customizing configuration-defined routes

The optional `customize` callback is invoked for every route after its `ScatterGatherOptions` has been built from configuration but before the endpoint is registered. Use it to add gatherers, override `UseOutputFormatters`, set a `CustomAggregator`, or apply any other per-route change:

<!-- snippet: scatter-gather-from-configuration-with-customization -->
<a id='snippet-scatter-gather-from-configuration-with-customization'></a>
```cs
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
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L58-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration-with-customization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `customize` callback receives the route template string, making it easy to apply different changes to different routes from the same call.

### Custom gatherer types

By default, every gatherer entry in configuration creates an `HttpGatherer`. To plug in a different implementation — such as an in-memory source, a database reader, or a third-party gatherer — add a `Type` field to the gatherer entry and register a factory for that type.

#### JSON configuration shape with a custom type

```json
{
  "ScatterGather": [
    {
      "Template": "api/products",
      "Gatherers": [
        { "Key": "ProductDetails", "DestinationUrl": "https://products.web.server/api/details" },
        { "Key": "StaticProductDetails", "Type": "StaticProductDetails" }
      ]
    }
  ]
}
```

When `Type` is omitted the entry behaves exactly as before (backward-compatible). When `Type` is present, the factory registered under that name is called with the raw `IConfigurationSection` for the entry, allowing access to any additional fields.

#### Implementing and registering the gatherer

Define the custom gatherer:

<!-- snippet: scatter-gather-custom-gatherer-type -->
<a id='snippet-scatter-gather-custom-gatherer-type'></a>
```cs
class StaticProductDetails(string key) : IGatherer
{
    public string Key { get; } = key;

    public Task<IEnumerable<object>> Gather(HttpContext context)
    {
        var data = (IEnumerable<object>)[new { Value = "InStockItem" }];
        return Task.FromResult(data);
    }
}
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L78-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-custom-gatherer-type' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Register the factory alongside the scatter/gather services in `ConfigureServices`:

<!-- snippet: scatter-gather-from-configuration-with-custom-type-services -->
<a id='snippet-scatter-gather-from-configuration-with-custom-type-services'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddHttpClient();
    services.AddScatterGather(config =>
    {
        config.AddGathererFactory(
            "StaticProductDetails",
            (section, _) => new StaticProductDetails(section["Key"]));
    });
}
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L107-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration-with-custom-type-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory receives:
- `IConfigurationSection` — the raw section for the gatherer entry (access any field via `section["FieldName"]`)
- `IServiceProvider` — the application's **root** (singleton) service provider

> **Lifetime note:** Factories are invoked once at startup, not per request. The `IServiceProvider` argument is the singleton root provider — resolving a scoped service (e.g. a `DbContext`) from it will either throw a scope-validation error or silently return a root-lifetime instance. If your gatherer needs per-request services, accept them via `HttpContext.RequestServices` inside `IGatherer.Gather` instead.

Then map as usual:

<!-- snippet: scatter-gather-from-configuration-with-custom-type-configure -->
<a id='snippet-scatter-gather-from-configuration-with-custom-type-configure'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IConfiguration configuration)
{
    app.UseRouting();
    app.UseEndpoints(builder =>
    {
        builder.MapScatterGather(configuration.GetSection("ScatterGather"));
    });
}
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L121-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration-with-custom-type-configure' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If a `Type` value is encountered in configuration but no matching factory has been registered, a descriptive `InvalidOperationException` is thrown at startup listing the missing type and how to register it.

#### Gatherers with extra configuration properties

Because the factory receives the raw `IConfigurationSection`, any additional fields present in the entry are available alongside `Key` and `Type`. Use `section["FieldName"]` for strings and `section.GetValue<T>("FieldName")` for typed values.

Given this configuration:

```json
{
  "ScatterGather": [
    {
      "Template": "api/products",
      "Gatherers": [
        { "Key": "Inventory", "Type": "FilteredInventory", "Category": "Electronics", "MaxItems": 10 }
      ]
    }
  ]
}
```

The gatherer reads those extra fields from its constructor:

<!-- snippet: scatter-gather-custom-gatherer-type-extra-properties -->
<a id='snippet-scatter-gather-custom-gatherer-type-extra-properties'></a>
```cs
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
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L91-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-custom-gatherer-type-extra-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

And the factory passes those values through at registration time:

<!-- snippet: scatter-gather-from-configuration-with-custom-type-extra-properties -->
<a id='snippet-scatter-gather-from-configuration-with-custom-type-extra-properties'></a>
```cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRouting();
    services.AddHttpClient();
    services.AddScatterGather(config =>
    {
        config.AddGathererFactory(
            "WithProperties",
            (section, _) => new GathererWithProperties(
                section["Key"],
                section["Category"],
                section.GetValue<int>("MaxItems")));
    });
}
```
<sup><a href='/src/Snippets/ScatterGather/ConfigurationBasedSetup.cs#L135-L150' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-from-configuration-with-custom-type-extra-properties' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
