using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ViewModelFactory
{
    // begin-snippet: view-model-factory-product-view-model-factory
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
    // end-snippet
}