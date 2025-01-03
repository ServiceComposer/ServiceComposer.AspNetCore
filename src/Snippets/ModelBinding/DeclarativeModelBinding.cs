using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

#pragma warning disable SC0001
namespace Snippets.ModelBinding
{
    class DeclarativeModelBindingUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-bind-body-and-route-data
        [HttpPost("/sample/{id}")]
        [BindFromBody<BodyModel>]
        [BindFromRoute<int>(routeValueKey: "id")]
        public Task Handle(HttpRequest request)
        {
            var ctx = request.GetCompositionContext();
            var arguments = ctx.GetArguments(GetType());
            
            var body = arguments.Argument<BodyModel>();
            var id = arguments.Argument<int>("id");

            //use values as needed
            
            return Task.CompletedTask;
        }
        // end-snippet
    }
}
#pragma warning restore SC0001