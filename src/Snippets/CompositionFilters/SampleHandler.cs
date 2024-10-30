using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.CompositionFilters
{
    // begin-snippet: composition-filter-attribute
    public class SampleCompositionFilterAttribute : CompositionRequestFilterAttribute
    {
        public override ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        {
            return next(context);
        }
    }
    // end-snippet
    
    // begin-snippet: composition-filter-class
    public class SampleCompositionFilter : ICompositionRequestFilter<SampleHandler>
    {
        public ValueTask<object> InvokeAsync(CompositionRequestFilterContext context, CompositionRequestFilterDelegate next)
        {
            return next(context);
        }
    }
    // end-snippet
    
    // begin-snippet: handler-with-composition-filter
    public class SampleHandler : ICompositionRequestsHandler
    {
        [SampleCompositionFilter]
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
}