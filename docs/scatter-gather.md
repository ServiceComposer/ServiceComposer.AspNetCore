# Scatter/Gather

ServiceCompose natively supports scatter/gather scenarios. Scatter/gather is supported through a fanout approach. Given an incoming HTTP request, ServiceComposer will issue as many downstream HTTP requests to fetch data from downstream endpoints. Once all data has been retrieved, they are composed and returned to the original upstream caller.

The following configuration configures a scatter/gather endpoint:

<!-- snippet: scatter-gather-basic-usage -->
<a id='snippet-scatter-gather-basic-usage'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseRouting();
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<Gatherer>
        {
            new HttpGatherer(key: "ASamplesSource", destinationUrl: "https://a.web.server/api/samples/ASamplesSource"),
            new HttpGatherer(key: "AnotherSamplesSource", destinationUrl: "https://another.web.server/api/samples/AnotherSamplesSource")
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/Startup.cs#L11-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-basic-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The above configuration snippet configures ServiceComposer to handle HTTP requests matching the template. Each time a matching request is dealt with, ServiceComposer invokes each configured gatherer and merges responses from each one into a response returned to the original issuer.

The `Key` and `Destination` properties are mandatory. The key uniquely identifies each gatherer in the context of a specific request. The destination is the downstream URL of the endpoint to invoke to retrieve data.

## Customizing downstream URLs

If the incoming request contains a query string, the query string and its values are automatically appended to downstream URLs as is. It is possible to override that behavior by setting the `DownstreamUrlMapper` delegate as presented in the following snippet:

<!-- snippet: scatter-gather-customizing-downstream-urls -->
<a id='snippet-scatter-gather-customizing-downstream-urls'></a>
```cs
public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
{
    app.UseEndpoints(builder => builder.MapScatterGather(template: "api/scatter-gather", new ScatterGatherOptions()
    {
        Gatherers = new List<Gatherer>
        {
            new HttpGatherer("ASamplesSource", "https://a.web.server/api/samples/ASamplesSource")
            {
                DestinationUrlMapper = (request, destination) => destination.Replace(
                    "{this-is-contextual}", 
                    request.HttpContext.Request.Query["this-is-contextual"])
            }
        }
    }));
}
```
<sup><a href='/src/Snippets/ScatterGather/CustomizingDownstreamURLs.cs#L10-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-customizing-downstream-urls' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same approach can be used to customize the downstream URL before invocation.

## Data format

ServiceComposer scatter/gather support works only with JSON data. Gatherers must return an `IEnumerable<JsonNode>`. By default, gatherers assume that the downstream endpoint result can be converted into a `JsonArray`.

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
public class CustomHttpGatherer : HttpGatherer
{
    public CustomHttpGatherer(string key, string destination) : base(key, destination) { }

    public override Task<IEnumerable<object>> Gather(HttpContext context)
    {

        return base.Gather(context);
    }
}
```
<sup><a href='/src/Snippets/ScatterGather/GatherMethodOverride.cs#L12-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-scatter-gather-gather-override' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
