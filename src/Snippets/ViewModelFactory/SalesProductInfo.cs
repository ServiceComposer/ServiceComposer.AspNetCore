using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.ViewModelFactory
{
    // begin-snippet: view-model-factory-sales-handler
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
    // end-snippet
}