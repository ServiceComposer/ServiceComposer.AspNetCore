<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway.

Designing a UI when the back-end system consists of dozens (or more) of (micro)services is challenging. We have separation and autonomy on the back end, but this all needs to come back together on the front-end. ViewModel Composition stops it from turning into a mess of spaghetti code and prevents simple actions from causing an inefficient torrent of web requests.

<!-- toc -->
## Contents

  * [Technical introduction](#technical-introduction)
  * [Getting Started](#getting-started)
  * [Documentation and supported platforms](#documentation-and-supported-platforms)
  * [Philosophy](#philosophy)
    * [Service boundaries](#service-boundaries)<!-- endToc -->

## Technical introduction

For a technical introduction and an overview of the problem space, refer to the following presentation available on [YouTube](https://www.youtube.com/watch?v=AxWGAiIg7_0).

## Getting Started

Imagine an elementary e-commerce web page, where it's needed to display details about a selected product. These details are stored in two different services. The Sales service owns the product price, and the Marketing service owns the product name and description. ServiceComposer solves the problem of composing information coming from different services into one composed view model that can be later displayed or consumed by downstream clients.

To start using ServiceComposer, follow the outlined steps:

- Create, in an empty or existing solution, a .NET Core 3.x or later empty web application project named `CompositionGateway`.
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

> NOTE: To use a `Startup` class, Generic Host support is required.

- Add a new .NET Core 3.x or later class library project, named `Sales.ViewModelComposition`.
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

- Make so that the web application project created at the beginning can load both class library assemblies, e.g., by adding a reference to the class library projects
- Build and run the web application project
- Using a browser or a tool like Postman, issue an HTTP Get request to `<url-of-the-web-application>/product/1`

The HTTP response should be a JSON result containing the properties and values defined in the composition handler classes.

> NOTE: ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required.

In this brief sample the view model instance returned by `GetComposedResponseModel()` is a C# `dynamic` object. `dynamic` objects are handy because they allow requests handlers to be fully independent from each other, they share nothing. ServiceComposer supports using strongly typed view models in case they are preferred. They come with the advantage of strong typing and compilers checks, and the disadvantage of a little coupling. Refer to the [view model factory documentation](docs/view-model-factory) for more information.

## Documentation and supported platforms

ServiceComposer is available for the following platforms:

- ASP.NET Core 3.x and .NET 5: [documentation is available in the docs folder](docs)
- ASP.NET Core 2.x: [documentation is available in the docs/asp-net-core-2x folder](docs/asp-net-core-2x) (.NET Standard 2.0 compatible)

> Note: Support for ASP.NET Core 2.x has been deprecated in version 1.8.0 and will be removed in 2.0.0.

## Philosophy

### Service boundaries

When building systems based on SOA principles, service boundaries are key, if not THE key aspect. If we get service boundaries wrong, the end result has the risk to be, in the best case, a distributed monolith, and in the worst one, a complete failure.

> Service boundaries identification is a challenge on its own; it requires a lot of business domain knowledge and a lot of confidence with high-level design techniques. Other than that, technical challenges might drive the solution design in the wrong direction due to the lack of technical solutions to problems foreseen while defining service boundaries.

The transition from the user mental model, described by domain experts, to the service boundaries architectural model in the SOA space raises many different concerns. If domain entities, as defined by domain experts, are split among several services:

- how can we then display to users what they need to visualize?
- when systems need to make decisions, how can they “query” data required to make that decision, stored in many different services?

This type of question leads systems to be designed using rich events, and not thin ones, to share data between services and at the same to share data with cache-like things, such as Elastic Search, to satisfy UI query/visualization needs.

This is the beginning of a road that can only lead to a distributed monolith, where data ownership is a lost concept and every change impacts and breaks the whole system. In such a scenario, it’s very easy to blame SOA and the toolset.

ViewModel Composition techniques are designed to address all these concerns. ViewModel Composition brings the separation of concerns, designed at the back-end, to the front-end.

For more details and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).
