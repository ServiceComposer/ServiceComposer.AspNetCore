# Response serialization casing

ServiceComposer serializes responses using a JSON serializer. By default responses are serialized using camel casing, a C# `SampleProperty` property is serialized as `sampleProperty`. Consumers can influence the response casing of a specific request by adding to the request an `Accept-Casing` custom HTTP header. Accepted values are `casing/camel` (default) and `casing/pascal`.

## Default response serialization casing

Default response serialization is camel casing. It's possible to configure a different default response serialization casing when configuring ServiceComposer:

snippet: net-core-3x-default-casing

Requests containing the `Accept-Casing` custom HTTP header will still be honored.
