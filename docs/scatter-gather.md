# Scatter/Gather

ServiceCompose natively supports scatter/gather scenarios. Scatter/gather is supported through a fanout approach. Given an incoming HTTP request, ServiceComposer will issue as many downstream HTTP requests to fetch data from downstream endpoints. Once all data has been retrieved, they are composed and returned to the original upstream caller.

The following configuration configures a scatter/gather endpoint:

snippet: scatter-gather-basic-usage

The above configuration snippet configures ServiceComposer to handle HTTP requests matching the template. Each time a matching request is dealt with, ServiceComposer invokes each configured gatherer and merges responses from each one into a response returned to the original issuer.

The `Key` and `Destination` properties are mandatory. The key uniquely identifies each gatherer in the context of a specific request. The destination is the downstream URL of the endpoint to invoke to retrieve data.

## Customizing downstream URLs

If the incoming request contains a query string, the query string and its values are automatically appended to downstream URLs as is. It is possible to override that behavior by setting the `DownstreamUrlMapper` delegate as presented in the following snippet:

snippet: scatter-gather-customizing-downstream-urls

The same approach can be used to customize the downstream URL before invocation.

## Data format

ServiceComposer scatter/gather support works only with JSON data. Gatherers must return an `IEnumerable<JsonNode>`. By default, gatherers assume that the downstream endpoint result can be converted into a `JsonArray`.

### Transforming returned data

TODO

### Taking control of the downstream invocation process

TODO
