<!--
GENERATED FILE - DO NOT EDIT
This file was generated by [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets).
Source File: /README.source.md
To change this file edit the source file and then run MarkdownSnippets.
-->

<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway. For more details, and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).

## Usage

### ASP.NET Core 3.x

ServiceComposer for ASP.NET Core 3.x levarages the new Endpoints support to plugin into the request handling pipeline.
ServiceComposer can be added to exsting or new ASP.NET Core projects, and to .NET Core console applications.

Add a reference to `ServiceComposer.AspNetCore` Nuget package and configure the `Startup` class like follows:

<!-- snippet: net-core-3x-sample-startup -->
<a id='snippet-net-core-3x-sample-startup'/></a>
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddViewModelComposition();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.UseRouting();
        app.UseEndpoints(builder => builder.MapCompositionHandlers());
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/Configuration/Startup.cs#L8-L23' title='File snippet `net-core-3x-sample-startup` was extracted from'>snippet source</a> | <a href='#snippet-net-core-3x-sample-startup' title='Navigate to start of snippet `net-core-3x-sample-startup`'>anchor</a></sup>
<!-- endsnippet -->

> NOTE: To use a `Startup` class Generic Host support is required.

ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required. For example to enable composition support for the `/sample/{id}` route an handler like the following can be defined:

<!-- snippet: net-core-3x-sample-handler -->
<a id='snippet-net-core-3x-sample-handler'/></a>
```cs
public class SampleHandler : ICompositionRequestsHandler
{
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/SampleHandler/SampleHandler.cs#L8-L17' title='File snippet `net-core-3x-sample-handler` was extracted from'>snippet source</a> | <a href='#snippet-net-core-3x-sample-handler' title='Navigate to start of snippet `net-core-3x-sample-handler`'>anchor</a></sup>
<!-- endsnippet -->

#### Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regaulr ASP.NET Core 3.x process and no special configuration is needed to plugin ServiceComposer.

### ASP.NET Core 2.x

Create a new .NET Core console project and add a reference to the following Nuget packages:

* `Microsoft.AspNetCore`
* `Microsoft.AspNetCore.Routing`
* `ServiceComposer.AspNetCore`

Configure the `Startup` class like follows:

<!-- snippet: net-core-2x-sample-startup -->
<a id='snippet-net-core-2x-sample-startup'/></a>
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddViewModelComposition();
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        app.RunCompositionGateway(routeBuilder =>
        {
            routeBuilder.MapComposableGet("{controller}/{id:int}");
        });
    }
}
```
<sup><a href='/src/Snippets.NetCore2x/Startup.cs#L9-L26' title='File snippet `net-core-2x-sample-startup` was extracted from'>snippet source</a> | <a href='#snippet-net-core-2x-sample-startup' title='Navigate to start of snippet `net-core-2x-sample-startup`'>anchor</a></sup>
<!-- endsnippet -->

> Note: define routes so to match your project needs. `ServiceComposer` adds provides all the required `MapComposable*` `IRouteBuilder` extension methods to map routes for every HTTP supported Verb.

Define one or more classes implementing either the `IHandleRequests` or the `ISubscribeToCompositionEvents` based on your needs.

> Make sure the assemblies containing requests handlers and events subscribers are available to the composition gateway. By adding a reference or by simply dropping assemblies in the `bin` directory.

More details on how to implement `IHandleRequests` and `ISubscribeToCompositionEvents` are available in the following articles:

* [ViewModel Composition: show me the code!](https://milestone.topics.it/view-model-composition/2019/03/06/viewmodel-composition-show-me-the-code.html)
* [The ViewModels Lists Composition Dance](https://milestone.topics.it/view-model-composition/2019/03/21/the-viewmodels-lists-composition-dance.html)

### MVC and Web API support

For information on how to host the Composition Gateway in a ASP.Net COre MVC application, please, refer to the [`ServiceComposer.AspNetCore.Mvc` package](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Mvc).

### Icon

[API](‪https://thenounproject.com/term/api/883169‬) by [Guilherme Simoes](https://thenounproject.com/uberux/) from [the Noun Project](https://thenounproject.com/).
