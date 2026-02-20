<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway.

Designing a UI when the back-end system consists of dozens (or more) of (micro)services is challenging. We have separation and autonomy on the back end, but this all needs to come back together on the front-end. ViewModel Composition prevents the back-end system from turning into a mess of spaghetti code and prevents simple actions from causing an inefficient torrent of web requests.

<!-- toc -->
## Contents

  * [Technical introduction](#technical-introduction)
  * [Getting Started](#getting-started)
  * [Documentation and supported platforms](#documentation-and-supported-platforms)
  * [Philosophy](#philosophy)
    * [Service Boundaries](#service-boundaries)<!-- endToc -->

## Technical introduction

For a technical introduction and an overview of the problem space, refer to the following presentation on [YouTube](https://www.youtube.com/watch?v=AxWGAiIg7_0).

## Getting Started

Imagine an elementary e-commerce web page where it's needed to display details about a selected product. These details are stored in two different services. The Sales service owns the product price, and the Marketing service owns the product name and description. ServiceComposer solves the problem of composing information from different services into one composed view model that downstream clients can later display or consume.

To start using ServiceComposer, follow the outlined steps:

- Create a .NET 10 empty web application project named `CompositionGateway` in an empty or existing solution.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package and configure the `Startup` class as follows:

<!-- snippet: sample-startup -->
<a id='snippet-sample-startup'></a>
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
<sup><a href='/src/Snippets/BasicUsage/Startup.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample-startup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Add a new .NET 10 class library project named `Sales.ViewModelComposition`.
- Add a package reference to the `ServiceComposer.AspNetCore` NuGet package.
- Add a new class to create a composition request handler.
- Define the class similar to the following:

<!-- snippet: basic-usage-sales-handler -->
<a id='snippet-basic-usage-sales-handler'></a>
```cs
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the sales database or service
        vm.ProductId = request.HttpContext.GetRouteValue("id").ToString();
        vm.ProductPrice = 100;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/BasicUsage/SalesProductInfo.cs#L9-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-basic-usage-sales-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Add another class library project, named `Marketing.ViewModelComposition`, and define a composition request handler like the following:

<!-- snippet: basic-usage-marketing-handler -->
<a id='snippet-basic-usage-marketing-handler'></a>
```cs
public class MarketingProductInfo: ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel();

        //retrieve product details from the marketing database or service
        vm.ProductName = "Sample product";
        vm.ProductDescription = "This is a sample product";
        
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/BasicUsage/MarketingProductInfo.cs#L8-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-basic-usage-marketing-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- Make it so that the web application project created at the beginning can load both class library assemblies, e.g., by adding a reference to the class library projects
- Build and run the web application project
- Using a browser or a tool like Postman, issue an HTTP Get request to `<url-of-the-web-application>/product/1`

The HTTP response should be a JSON result containing the properties and values defined in the composition handler classes.

> [!NOTE]
> ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.

In this brief sample, the view model instance returned by `GetComposedResponseModel()` is a C# `dynamic` object. `dynamic` objects are handy because they allow request handlers to be entirely independent of each other; they share nothing. ServiceComposer supports using strongly typed view models if they are preferred. They have the advantages of strong typing and compiler checks and the disadvantages of a bit of coupling. Refer to the [view model factory documentation](docs/view-model-factory.md) for more information.

## Documentation and supported platforms

ServiceComposer is available for the following platforms:

- ASP.NET Core on .NET 10: [documentation is available in the docs folder](docs)

## Philosophy

### Service Boundaries

Service boundaries are essential, if not THE critical aspect, when building systems based on SOA principles. If we get service boundaries wrong, the end result risks being a distributed monolith in the best case and a complete failure in the worst case.

> Service boundary identification is challenging; it requires extensive business domain knowledge and confidence in high-level design techniques. Technical challenges, such as the lack of technical solutions to problems foreseen while defining service boundaries, might drive the solution design in the wrong direction.

The transition from the user mental model, described by domain experts, to the service boundaries architectural model in the SOA space raises many different concerns. If domain entities, as defined by domain experts, are split among several services:

- how can we then display to users what they need to visualize?
- when systems need to make decisions, how can they “query” data required to make that decision, stored in many different services?

This type of question leads systems to be designed using rich events, not thin ones, to share data between services and with cache-like things, such as Elastic Search, to satisfy UI query/visualization needs.

This is the beginning of a road that can only lead to a distributed monolith, where data ownership is a lost concept and every change impacts and breaks the whole system. In such a scenario, it’s easy to blame SOA and the toolset.

ViewModel Composition techniques are designed to address all these concerns. They bring the separation of concerns, designed at the back end, to the front end.

For more details and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).
