<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway. For more details, and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).

## Usage

### ASP.NET Core 2.x

Create a new .NET Core console project and add a reference to the following Nuget packages:

* `Microsoft.AspNetCore`
* `Microsoft.AspNetCore.Routing`
* `ServiceComposer.AspNetCore`

Configure the `Startup` class like following:

snippet: net-core-2x-sample-startup

> Note: define routes so to match your project needs. `ServiceComposer` adds many `MapComposable*` extension methods to the `IRouteBuilder` interface, to map routes for every HTTP supported Verb.

Define one or more classes implementing either the `IHandleRequests` or the `ISubscribeToCompositionEvents` based on your needs.

> Make sure the assemblies containing requests handlers and events subscribers are available to the composition gateway. By adding a reference or by simply dropping assemblies in the `bin` directory.

More details on how to implement `IHandleRequests` and `ISubscribeToCompositionEvents` are available in the following articles:

* [ViewModel Composition: show me the code!](https://milestone.topics.it/view-model-composition/2019/03/06/viewmodel-composition-show-me-the-code.html)
* [The ViewModels Lists Composition Dance](https://milestone.topics.it/view-model-composition/2019/03/21/the-viewmodels-lists-composition-dance.html)

### MVC and Web API support

For information on how to host the Composition Gateway in a ASP.Net COre MVC application, please, refer to the [`ServiceComposer.AspNetCore.Mvc` package](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Mvc).

### Icon

[API](‪https://thenounproject.com/term/api/883169‬) by [Guilherme Simoes](https://thenounproject.com/uberux/) from [the Noun Project](https://thenounproject.com/).
