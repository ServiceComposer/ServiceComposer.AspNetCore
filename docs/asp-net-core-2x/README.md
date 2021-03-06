<!--
GENERATED FILE - DO NOT EDIT
This file was generated by [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets).
Source File: /docs/asp-net-core-2x/README.source.md
To change this file edit the source file and then run MarkdownSnippets.
-->

# ASP.NET Core 2.x

_NOTE: As of v1.8.0 ASP.NET Core 2.x is legacy. It'll be removed in v2.0.0, consider moving to the new endpoint and attribute routing based approach available starting with ASP.NET Core 3.x._

Create a new .NET Core console project and add a reference to the following NuGet packages:

* `Microsoft.AspNetCore`
* `Microsoft.AspNetCore.Routing`
* `ServiceComposer.AspNetCore`

Configure the `Startup` class like follows:

<!-- snippet: net-core-2x-sample-startup -->
<a id='snippet-net-core-2x-sample-startup'></a>
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
<sup><a href='/src/Snippets.NetCore2x/Startup.cs#L9-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-2x-sample-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> Note: define routes so to match your project needs. `ServiceComposer` adds provides all the required `MapComposable*` `IRouteBuilder` extension methods to map routes for every HTTP supported Verb.

Define one or more classes implementing either the `IHandleRequests` or the `ISubscribeToCompositionEvents` based on your needs.

> Make sure the assemblies containing requests handlers and events subscribers are available to the composition gateway. By adding a reference or by simply dropping assemblies in the `bin` directory.

More details on how to implement `IHandleRequests` and `ISubscribeToCompositionEvents` are available in the following articles:

* [ViewModel Composition: show me the code!](https://milestone.topics.it/view-model-composition/2019/03/06/viewmodel-composition-show-me-the-code.html)
* [The ViewModels Lists Composition Dance](https://milestone.topics.it/view-model-composition/2019/03/21/the-viewmodels-lists-composition-dance.html)

## Custom HTTP status codes in ASP.NET Core 2.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

<!-- snippet: net-core-2x-sample-handler-with-custom-status-code -->
<a id='snippet-net-core-2x-sample-handler-with-custom-status-code'></a>
```cs
public class SampleHandlerWithCustomStatusCode : IHandleRequests
{
    public bool Matches(RouteData routeData, string httpVerb, HttpRequest request)
    {
        return true;
    }

    public Task Handle(string requestId, dynamic vm, RouteData routeData, HttpRequest request)
    {
        var response = request.HttpContext.Response;
        response.StatusCode = (int)HttpStatusCode.Forbidden;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore2x/SampleHandler/SampleHandler.cs#L9-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-2x-sample-handler-with-custom-status-code' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.

## MVC and Web API support

For information on how to host the Composition Gateway in a ASP.Net Core MVC application, please, refer to the [`ServiceComposer.AspNetCore.Mvc` package](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Mvc).
