# Single item and list composition

ViewModel Composition serves two kinds of pages: a single item, e.g., a product details page, and a list of items, e.g., a products list. Composing a single item only needs composition handlers. Composing a list also needs [events](events.md).

## Single item composition

Each service registers a composition handler for the item's route. They all know the item's identifier, from the route, so each one independently retrieves its own data and adds it to the view model:

- Sales adds the product price.
- Marketing adds the product name and description.

The [getting started guide](getting-started.md#writing-composition-handlers) walks through this case, with the full code of both handlers.

## List composition

Composing a list is different: only one service knows which items are in the list. The others can't retrieve their data until they know the items' identifiers. For example, Marketing owns the products catalog, so it decides which products the `/products` page lists, and Sales adds a price to each of them.

The service owning the list:

1. Retrieves the items, and creates a view model for each of them.
2. Adds the list to the response view model.
3. Publishes an event with the items, keyed by identifier, so that the other services can add their data.

The event carries the items:

<!-- snippet: list-composition-event -->
<a id='snippet-list-composition-event'></a>
```cs
public record ProductsLoaded(IReadOnlyDictionary<int, dynamic> Products);
```
<sup><a href='/src/Snippets/ListComposition/ProductsLoaded.cs#L5-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-list-composition-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The Marketing composition handler owns the list:

<!-- snippet: list-composition-owner -->
<a id='snippet-list-composition-owner'></a>
```cs
public class MarketingProductsList : ICompositionRequestsHandler
{
    [HttpGet("/products")]
    public async Task Handle(HttpRequest request)
    {
        // retrieve the products list from the marketing database or service
        var products = new Dictionary<int, dynamic>();
        foreach (var (id, name) in new[] { (1, "Sample product"), (2, "Another product") })
        {
            dynamic product = new ExpandoObject();
            product.Id = id;
            product.Name = name;
            products.Add(id, product);
        }

        var vm = request.GetComposedResponseModel();
        vm.Products = products.Values.ToList();

        // let the other services add their data to each product
        await request.GetCompositionContext().RaiseEvent(new ProductsLoaded(products));
    }
}
```
<sup><a href='/src/Snippets/ListComposition/MarketingProductsList.cs#L11-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-list-composition-owner' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The other services handle the event with a [generic event handler](events.md#generic-event-handlers). The Sales one adds the prices:

<!-- snippet: list-composition-contributor -->
<a id='snippet-list-composition-contributor'></a>
```cs
public class SalesProductsPrices : ICompositionEventsHandler<ProductsLoaded>
{
    public Task Handle(ProductsLoaded @event, HttpRequest request)
    {
        // retrieve the prices of all the products at once, from the
        // sales database or service, e.g., GET /prices?ids=1,2
        var ids = @event.Products.Keys.ToArray();
        var prices = ids.ToDictionary(id => id, id => 100m + id);

        foreach (var (id, product) in @event.Products)
        {
            product.Price = prices[id];
        }

        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Snippets/ListComposition/SalesProductsPrices.cs#L8-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-list-composition-contributor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The response is the list of products, each one composed by both services:

```json
{
  "products": [
    { "id": 1, "name": "Sample product", "price": 101 },
    { "id": 2, "name": "Another product", "price": 102 }
  ]
}
```

A few guidelines:

- **Retrieve the data for all the items at once.** Each service handling the event gets all the identifiers, so it can make one call to its database or service, instead of one per item.
- **Each service owns its own properties.** Event handlers run in parallel, and they all write to the same items. Writing distinct properties is safe, but reading and writing the same one isn't. See [thread safety](thread-safety.md#composition-events).
- **The event is the contract.** Services handling it don't need to know the route of the page. If another page lists products, publishing the same event composes it the same way.

> [!NOTE]
> For more about composing lists and the related challenges, read the [Into the darkness of ViewModels Lists Composition](https://milestone.topics.it/2019/02/28/into-the-darkness-of-viewmodel-lists-composition.html) blog post.
