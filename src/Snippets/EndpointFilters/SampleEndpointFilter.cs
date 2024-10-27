using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Snippets.EndpointFilters;

// begin-snippet: sample-endpoint-filter
class SampleEndpointFilter : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Do something meaningful prior to invoking the rest of the pipeline
        
        var response = await next(context);

        // Do something meaningful with the response

        return response;
    }
}
// end-snippet