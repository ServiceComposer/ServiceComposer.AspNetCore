# ASP.NET Core 3.x and .NET 5

Starting ASP.NET Core 3.x ServiceComposer leverages the new Endpoints support to plugin into the request handling pipeline.
ServiceComposer can be added to existing or new ASP.NET Core projects, and to .NET Core console applications.

Add a reference to `ServiceComposer.AspNetCore` NuGet package and configure the `Startup` class like follows:

snippet: net-core-3x-sample-startup

> NOTE: To use a `Startup` class Generic Host support is required.

ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required. For example, to enable composition support for the `/sample/{id}` route an handler like the following can be defined:

snippet: net-core-3x-sample-handler

## Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For example, it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regular ASP.NET Core 3.x process and no special configuration is needed to plugin ServiceComposer:

snippet: net-core-3x-sample-handler-with-authorization

## Custom HTTP status codes in ASP.NET Core 3.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

snippet: net-core-3x-sample-handler-with-custom-status-code

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.
