using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.BasicUsage
{
    // begin-snippet: net-core-3x-basic-usage-marketing-handler
    public class MarketingProductInfo: ICompositionRequestsHandler
    {
        [HttpGet("/product/{id}")]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();

            vm.ProductName = "Sample product";
            vm.ProductDescription = "This is a sample product";
            
            return Task.CompletedTask;
        }
    }
    // end-snippet
}