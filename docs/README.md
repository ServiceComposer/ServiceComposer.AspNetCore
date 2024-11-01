# Documentation

The [getting started](getting-started.md) documentation provides an overview of the basic building blocks:

- [Hosting](getting-started.md)
- [Composition requests handlers](getting-started.md#composition-requests-handlers)
- [Composition events handlers](getting-started.md#composition-events-handlers)

## Supported .NET versions

ServiceComposer targets .NET 8.

> [!NOTE]
> About thread safety
> If resources are shared across more than one handler they must be [thread-safe](thread-safety.md).

## Upgrade guides

[Upgrade guides](upgrade-guides) are available to ease the migration from one version to another.

## ASP.Net Model Binding

When handling composition requests it possible to leverage the power of ASP.Net Model Binding to bind incoming forms, bodies, query string parameters, or route data to strongly typed C# models. For more information on model binding refer to the [Model Binding](model-binding.md) section.

### ASP.Net MVC Action results

MVC Action results support allow composition handlers to set custom response results for specific scenarios, like for example, handling bad requests or validation error that would normally require throwing an exception. For more information on action results refer to the [MVC Action results](action-results.md) section.

## Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For more information refer to the [Authentication and Authorization](authentication-authorization.md) section

## Serialization

By default ServiceComposer serializes responses using the Newtonsoft JSON serializer. The built-in serialization support can be configured to serialize responses using a camel case or pascal case approach on a per request basis by adding to the request an `Accept-Casing` custom HTTP header. For more information refer to the [response serialization casing](response-serialization-casing.md) section. Or it's possible to take full control over the [response serialization settings on a case-by-case](custom-json-response-serialization-settings.md) by supplying at configuration time a customization function.

Starting with version 1.9.0, regular MVC Output Formatters can be used to serialize the response model, and honor the `Accept` HTTP header set by clients. When using output formatters the serialization casing is controlled by the formatter configuration and not by ServiceComposer. For more information on using output formatters refers to the [output formatters serialization section](output-formatters-serialization.md).

## Customizing ViewModel Composition options from dependent assemblies

It's possible to access and [customize ViewModel Composition options](options-customizations.md) at application start-up by defining types implementing the `IViewModelCompositionOptionsCustomization` interface.

## Endpoint filters

Endpoint filters allow intercepting all incoming HTTP requests before they reach the composition stage. For more information refer to the [endpoint filters documentation](endpoint-filters.md).

## Composition requests filters

Composition requests filters allow intercepting compostion requests before they are dispatched to composition handlers. For more information refer to the [Composition requests filters documentation](composition-filters.md).
