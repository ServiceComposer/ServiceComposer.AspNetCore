# Custom JSON response serialization settings

By default each response is serialized using [Json.Net](https://www.newtonsoft.com/json/help/html/Introduction.htm) and serialization settings (`JsonSerializerSettings`) are determined by the [requested response casing](response-serialization-casing.source.md). If the requested casing is camel casing, the default, the folowing serialization settings are applied to the response:

snippet: net-core-3x-camel-serialization-settings

If the requested case is pascal, the following settings are applied:

snippet: net-core-3x-pascal-serialization-settings

It's possible to customize the response serialization settings on a case-by-case using the following configuration:

snippet: net-core-3x-custom-serialization-settings

Each time ServiceComposer needs to serialize a response it'll invoke the supplied function.

NOTE:
When customizing the serialization settings, it's responsibility of the function to configure the correct resolver for the requested casing
