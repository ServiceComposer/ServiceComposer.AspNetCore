# Strongly typed view models

_Available starting with v1.8.0_

By default ServiceComposer uses C# `dynamic` object instances to serve view models to requests handler, when the `SalesProductInfo` handler, used in the getting started sample, requires the view model, the call to `GetComposedResponseModel()` returns a `dynamic` instance.

The first step to use strongly typed view models is to define the view model class:

<!-- snippet: view-model-factory-product-view-model -->
<a id='snippet-view-model-factory-product-view-model'></a>
```cs
public class ProductViewModel
{
    public string ProductId { get; set; }
    public decimal ProductPrice { get; set; }
    public string ProductName { get; set; }
    public string ProductDescription { get; set; }
}
```
<sup><a href='/src/Snippets.NetCore3x/ViewModelFactory/ProductViewModel.cs#L3-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-view-model-factory-product-view-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

View models have no requirements other than being serializable. They are POC objects.

## Endpoint scoped view model factories

The second step is to define a factory for the view model. Create a class the implements the `IEndpointScopedViewModelFactory` interface:

<!-- snippet: view-model-factory-product-view-model-factory -->
<a id='snippet-view-model-factory-product-view-model-factory'></a>
```cs
class ProductViewModelFactory : IEndpointScopedViewModelFactory
{
    [HttpGet("/product/{id}")]
    public object CreateViewModel(HttpContext httpContext, ICompositionContext compositionContext)
    {
        var productId = httpContext.GetRouteValue("id").ToString();
        return new ProductViewModel()
        {
            ProductId = productId
        };
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/ViewModelFactory/ProductViewModelFactory.cs#L8-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-view-model-factory-product-view-model-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

View model factories are scoped to endpoints, they need to be identified when handling routes and thus they need to be decorated with a routing attribute and a route template. Each rout that needs a strongly typed view model needs a different factory.

Once the factory is defined it's automatically registered in the DI container at startup and will be used by ServiceComposer when handling the specified route.

To use the strongly typed view model convert requests handlers to use the generic `GetComposedResponseModel<T>()` method, where the `T` parameter is the view model of choice:

<!-- snippet: view-model-factory-sales-handler -->
<a id='snippet-view-model-factory-sales-handler'></a>
```cs
public class SalesProductInfo : ICompositionRequestsHandler
{
    [HttpGet("/product/{id}")]
    public Task Handle(HttpRequest request)
    {
        var vm = request.GetComposedResponseModel<ProductViewModel>();

        //retrieve product details from the sales database or service
        vm.ProductPrice = 100;

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets.NetCore3x/ViewModelFactory/SalesProductInfo.cs#L8-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-view-model-factory-sales-handler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Global view model factories

As already mentioned the default ServiceComposer behavior if no endpoint scoped factories are registered is to create a `dynamic` object instance. It's possible to replace this behavior by defining a class the implements the `IViewModelFactory` interface. If an `IViewModelFactory` type is defined it'll be used to create all view models not handled by endpoint scoped factories.

## Resolution order

View model factories are resolved, and used, in the following order:

1. `IEndpointScopedViewModelFactory`: If an endpoint scoped factory is defined for the current route it'll be used to create a view model instance
2. `IViewModelFactory`: If no endpoint scoped factory exist bound to the current route and a global view model factory is registered it'll be used
3. If no global view model factory exist the default ServiceComposer factory creates a `dynamic` view model instance to use to serve the current request
