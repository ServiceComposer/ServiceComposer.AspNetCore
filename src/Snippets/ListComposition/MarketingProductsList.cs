using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.ListComposition;

// begin-snippet: list-composition-owner
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
// end-snippet
