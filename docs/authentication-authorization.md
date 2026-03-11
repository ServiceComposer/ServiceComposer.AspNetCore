# Authentication and Authorization

By virtue of leveraging ASP.NET Core Endpoints, ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For example, it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regular ASP.NET Core process and no special configuration is needed to plug in ServiceComposer:

<!-- snippet: sample-handler-with-authorization -->
<a id='snippet-sample-handler-with-authorization'></a>
```cs
public class SampleHandlerWithAuthorization : ICompositionRequestsHandler
{
    [Authorize]
    [HttpGet("/sample/{id}")]
    public Task Handle(HttpRequest request)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/SampleHandler/SampleHandler.cs#L10-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-handler-with-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## How it works

ServiceComposer registers composition endpoints using ASP.NET Core's endpoint routing system. Any endpoint metadata attributes — `[Authorize]`, `[AllowAnonymous]`, `[RequireAuthorization]`, custom policy attributes — are collected from all handlers registered for a route and merged onto the composed endpoint.

This means authorization is evaluated **before** composition handlers execute, as part of the standard ASP.NET Core middleware pipeline. If the request is not authorized, it is rejected and no composition handlers run.

## Multiple handlers with different authorization requirements

When multiple handlers are registered for the same route, their authorization metadata is merged. If any handler declares `[Authorize]`, the route requires authorization. The most restrictive combination applies.

For example, if one handler requires an authenticated user and another requires a specific policy, both requirements are enforced:

<!-- snippet: multiple-handlers-different-auth-requirements -->
<a id='snippet-multiple-handlers-different-auth-requirements'></a>
```cs
public class SalesHandler : ICompositionRequestsHandler
{
    [Authorize]
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request) { /* ... */ return Task.CompletedTask; }
}

public class InventoryHandler : ICompositionRequestsHandler
{
    [Authorize(Policy = "WarehouseStaff")]
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request) { /* ... */ return Task.CompletedTask; }
}
```
<sup><a href='/src/Snippets/Authentication/AuthenticationSnippets.cs#L11-L25' title='Snippet source file'>snippet source</a> | <a href='#snippet-multiple-handlers-different-auth-requirements' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Both `[Authorize]` and `[Authorize(Policy = "WarehouseStaff")]` are collected and applied to the route, so callers must satisfy both requirements.

## Setup

No special configuration is required beyond the standard ASP.NET Core authentication/authorization setup. Add authentication and authorization middleware before `app.MapCompositionHandlers()`:

<!-- snippet: auth-middleware-setup -->
<a id='snippet-auth-middleware-setup'></a>
```cs
var builder = WebApplication.CreateBuilder();
builder.Services.AddViewModelComposition();
builder.Services.AddAuthentication(); // configure your scheme here
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapCompositionHandlers();
app.Run();
```
<sup><a href='/src/Snippets/Authentication/AuthenticationSnippets.cs#L31-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-auth-middleware-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
