<img src="img/logo.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway.

Designing a UI when the back-end system consists of dozens (or more) of (micro)services is a challenge. We have separation and autonomy on the back end, but on the front-end this all needs to come back together. ViewModel Composition stops it from turning into a mess of spaghetti code, and prevents simple actions from causing an inefficient torrent of web requests.

When building systems based on SOA principles service boundaries are a key aspect, if not THE key aspect. If we get service boundaries wrong the end result has the risk to be, in the best case, a distributed monolith, and in the worst one, a complete failure.

> Service boundaries identification is a challenge on its own, it requires a lot of business domain knowledge and a lot of confidence with high level design techniques. Other than that there are technical challenges that might drive the solution design in the wrong direction due to the lack of technical solutions to problems foreseen while defining service boundaries.

The transition from the user mental model, what the domain expert describes, to the service boundaries architectural model in the SOA space raises many different concerns. If domain entities, as described by domain experts, are split among several different services:

- how can we then display to users what they need to visualize?
- when systems need to make decisions how can they “query” data, required to make that decision, stored in many different services?

This type of questions lead systems to be designed using rich events, and not thin ones, in order to share data between services and at the same to share data with cache-like things, such as Elastic Search, to satisfy UI query/visualization needs.

This is the beginning of a road that can only lead to a distributed monolith, where data ownership is a lost concept and every change impacts and breaks the whole system. In such a scenario it’s very easy to blame SOA and the tool set.

ViewModel Composition techniques are designed to address all these concerns. ViewModel Composition brings the separation of concerns, desinged at the back-end, to the front-end.

For more details, and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).

## Usage

ServiceComposer is available for the following platforms:

- [ASP.NET Core 3.x and .NET 5](asp-net-core-3x)
- [ASP.NET Core 2.x](asp-net-core-2x) (.NET Standard 2.0 compatible)

Note: Support for ASP.NET Core 2.x will be deprecated in version 1.8.0 and removed in 2.0.0.
