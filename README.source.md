<img src="assets/ServiceComposer.png" width="100" />

# ServiceComposer

ServiceComposer is a ViewModel Composition Gateway. For more details, and the philosophy behind a Composition Gateway, refer to the [ViewModel Composition series](https://milestone.topics.it/categories/view-model-composition) of article available on [milestone.topics.it](https://milestone.topics.it/).

## Usage

### ASP.NET Core 3.x

ServiceComposer for ASP.NET Core 3.x levarages the new Endpoints support to plugin into the request handling pipeline.
ServiceComposer can be added to exsting or new ASP.NET Core projects, and to .NET Core console applications.

Add a reference to `ServiceComposer.AspNetCore` Nuget package and configure the `Startup` class like follows:

snippet: net-core-3x-sample-startup

> NOTE: To use a `Startup` class Generic Host support is required.

ServiceComposer uses regular ASP.NET Core attribute routing to configure routes for which composition support is required. For example, to enable composition support for the `/sample/{id}` route an handler like the following can be defined:

snippet: net-core-3x-sample-handler

#### Authentication and Authorization

By virtue of leveraging ASP.NET Core 3.x Endpoints ServiceComposer automatically supports authentication and authorization metadata attributes to express authentication and authorization requirements on routes. For example, it's possible to use the `Authorize` attribute to specify that a handler requires authorization. The authorization process is the regular ASP.NET Core 3.x process and no special configuration is needed to plugin ServiceComposer:

snippet: net-core-3x-sample-handler-with-authorization

#### Custom HTTP status codes in ASP.NET Core 3.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

snippet: net-core-3x-sample-handler-with-custom-status-code

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.

### ASP.NET Core 2.x

Create a new .NET Core console project and add a reference to the following Nuget packages:

* `Microsoft.AspNetCore`
* `Microsoft.AspNetCore.Routing`
* `ServiceComposer.AspNetCore`

Configure the `Startup` class like follows:

snippet: net-core-2x-sample-startup

> Note: define routes so to match your project needs. `ServiceComposer` adds provides all the required `MapComposable*` `IRouteBuilder` extension methods to map routes for every HTTP supported Verb.

Define one or more classes implementing either the `IHandleRequests` or the `ISubscribeToCompositionEvents` based on your needs.

> Make sure the assemblies containing requests handlers and events subscribers are available to the composition gateway. By adding a reference or by simply dropping assemblies in the `bin` directory.

More details on how to implement `IHandleRequests` and `ISubscribeToCompositionEvents` are available in the following articles:

* [ViewModel Composition: show me the code!](https://milestone.topics.it/view-model-composition/2019/03/06/viewmodel-composition-show-me-the-code.html)
* [The ViewModels Lists Composition Dance](https://milestone.topics.it/view-model-composition/2019/03/21/the-viewmodels-lists-composition-dance.html)

#### Custom HTTP status codes in ASP.NET Core 2.x

The response status code can be set in requests handlers and it'll be honored by the composition pipeline. To set a custom response status code the following snippet can be used:

snippet: net-core-2x-sample-handler-with-custom-status-code

NOTE: Requests handlers are executed in parallel in a non-deterministic way, setting the response code in more than one handler can have unpredictable effects.

### MVC and Web API support

For information on how to host the Composition Gateway in a ASP.Net COre MVC application, please, refer to the [`ServiceComposer.AspNetCore.Mvc` package](https://github.com/ServiceComposer/ServiceComposer.AspNetCore.Mvc).

### Icon

[API](‪https://thenounproject.com/term/api/883169‬) by [Guilherme Simoes](https://thenounproject.com/uberux/) from [the Noun Project](https://thenounproject.com/).
