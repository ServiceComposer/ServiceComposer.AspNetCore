using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.ListComposition;

// begin-snippet: list-composition-contributor
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
// end-snippet
