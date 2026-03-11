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

Consider an e-commerce product page that needs data from two services: the Sales service owns the price, and the Marketing service owns the name and description. Each service defines an independent composition handler that contributes its slice of data to a shared view model:

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

Both handlers target the same route and run in parallel. Neither knows about the other. The gateway merges their contributions and returns a single JSON response to the caller.

For a complete walkthrough — installation, project structure, and next steps — see the **[getting started guide](docs/getting-started.md)**.

## Documentation and supported platforms

ServiceComposer is available for the following platforms:

- ASP.NET Core on .NET 10: [documentation is available in the docs folder](docs)
- New to ServiceComposer? Start with the [getting started guide](docs/getting-started.md).

## Philosophy

### Service Boundaries

Service boundaries are essential, if not THE critical aspect, when building systems based on SOA principles. If we get service boundaries wrong, the end result risks being a distributed monolith in the best case and a complete failure in the worst case.

> Service boundary identification is challenging; it requires extensive business domain knowledge and confidence in high-level design techniques. Technical challenges, such as the lack of technical solutions to problems foreseen while defining service boundaries, might drive the solution design in the wrong direction.

The transition from the user mental model, described by domain experts, to the service boundaries architectural model in the SOA space raises many different concerns. If domain entities, as defined by domain experts, are split among several services:

- how can we then display to users what they need to visualize?
- when systems need to make decisions, how can they "query" data required to make that decision, stored in many different services?

This type of question leads systems to be designed using rich events, not thin ones, to share data between services and with cache-like things, such as Elastic Search, to satisfy UI query/visualization needs.

This is the beginning of a road that can only lead to a distributed monolith, where data ownership is a lost concept and every change impacts and breaks the whole system. In such a scenario, it's easy to blame SOA and the toolset.

ViewModel Composition techniques are designed to address all these concerns. They bring the separation of concerns, designed at the back end, to the front end.

For more details and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).
