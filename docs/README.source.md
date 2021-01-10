# ASP.NET Core 3.x and .NET 5

Starting ASP.NET Core 3.x ServiceComposer leverages the new Endpoints support to plugin into the request handling pipeline.
ServiceComposer can be added to existing or new ASP.NET Core projects, or it can be hosted in .NET Core console applications.

## Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For more information refer to the [Authentication and Authorization](authentication-authorization.source.md) section

## Response serialization casing

ServiceComposer serializes responses using a JSON serializer. By default responses are serialized using camel casing. Consumers can influence the response casing of a specific request by adding to the request an `Accept-Casing` custom HTTP header. For more information refer to the [response serialization casing](response-serialization-casing.source.md) section.