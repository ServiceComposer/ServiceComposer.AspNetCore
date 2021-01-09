# Strongly typed view models

By default ServiceComposer uses C# `dynamic` object instances to serve view models to requests handler, when the `SalesProductInfo` handler, used in the getting started sample, requires the view model, the call to `GetComposedResponseModel()` returns a `dynamic` instance.

The first step to use strongly typed view models is to define the view model class:

snippet: view-model-factory-product-view-model

View models have no requirements other than being serializable. They are POC objects.
The second step is to define a factory for the view model. Create a class the implements the `IEndpointScopedViewModelFactory` interface:

snippet: view-model-factory-product-view-model-factory

View model factories are scoped to endpoints, they need to be identified when handling routes and thus they need to be decorated with a routing attribute and a route template. Each rout that needs a strongly typed view model needs a different factory.

Once the factory is defined it's automatically registered in the DI container at startup and will be used by ServiceComposer when handling the specified route.

To use the strongly typed view model convert requests handlers to use the generic `GetComposedResponseModel<T>()` method, where the `T` parameter is the view model of choice:

snippet: view-model-factory-sales-handler
