# Basic usage

To start using ServiceComposer follow the outlined steps:

- Create, in an empty or existing solution, a .NET Core 3.x or later empty web application project.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package and configure the `Startup` class like follows:

<!-- snippet: net-core-3x-sample-startup -->
<a id='snippet-net-core-3x-sample-startup'></a>
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
<sup><a href='/src/Snippets.NetCore3x/Configuration/Startup.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-sample-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> NOTE: To use a `Startup` class Generic Host support is required.

- Add a new .NET Core 3.x or later class library project.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package.
- Add a new class to create a composition request handler.
- Define the class similar to the following:

<!-- snippet: net-core-3x-sample-handler -->
<a id='snippet-net-core-3x-sample-handler'></a>
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
<sup><a href='/src/Snippets.NetCore3x/SampleHandler/SampleHandler.cs#L10-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-net-core-3x-sample-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Make so that the web application project created at the beginning can load the class library assembly, e.g. by adding a reference to the class library project
- Build and run the web application project
- Using a browser or a tool like Postman issue an HTTP Get request to `url-of-the-web-application/sampple/1`

The HTTP respinse should be a json result containing the properties and values defined in the composition handler class.

NOTE: ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.
