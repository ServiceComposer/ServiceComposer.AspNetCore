using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.BasicUsage
{
    // begin-snippet: net-core-3x-basic-usage-sales-handler
    public class SalesProductInfo : ICompositionRequestsHandler
    {
        [HttpGet("/product/{id}")]
        public Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();

            vm.ProductId = request.HttpContext.GetRouteValue("id").ToString();
            vm.ProductPrice = 100;

            return Task.CompletedTask;
        }
    }
    // end-snippet
}