<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway.

Designing a UI when the back-end system consists of dozens (or more) of (micro)services is challenging. We have separation and autonomy on the back end, but this all needs to come back together on the front-end. ViewModel Composition stops it from turning into a mess of spaghetti code and prevents simple actions from causing an inefficient torrent of web requests.

## Technical introduction

For a technical introduction and an overview of the problem space, refer to the following presentation available on [YouTube](https://www.youtube.com/watch?v=AxWGAiIg7_0):

## Usage

ServiceComposer is available for the following platforms:

- [ASP.NET Core 3.x and .NET 5](docs/asp-net-core-3x)
- [ASP.NET Core 2.x](docs/asp-net-core-2x) (.NET Standard 2.0 compatible)

_Note_: Support for ASP.NET Core 2.x will be deprecated in version 1.8.0 and removed in 2.0.0.

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
