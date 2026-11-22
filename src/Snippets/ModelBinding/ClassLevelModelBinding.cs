using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServiceComposer.AspNetCore;

#pragma warning disable SC0001
namespace Snippets.ModelBinding;

// begin-snippet: class-level-model-binding
[BindFromRoute<int>(routeValueKey: "id")]
public class OrderHandler : ICompositionRequestsHandler
{
    [HttpGet("/orders/{id}")]
    [BindFromQuery<int>(queryParameterName: "page")]
    public Task Handle(HttpRequest request)
    {
        var arguments = request.GetCompositionContext().GetArguments(this);
        var id = arguments.Argument<int>("id", BindingSource.Path);
        var page = arguments.Argument<int>("page", BindingSource.Query);

        return Task.CompletedTask;
    }
}
// end-snippet
