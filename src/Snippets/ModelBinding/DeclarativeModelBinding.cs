using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

#pragma warning disable SC0001
namespace Snippets.ModelBinding
{
    class DeclarativeModelBindingUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: declarative-model-binding
        [HttpPost("/sample/{id}")]
        [BindFromBody<BodyModel>]
        [BindFromRoute<int>(routeValueKey: "id")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
        // end-snippet
    }
}
#pragma warning restore SC0001