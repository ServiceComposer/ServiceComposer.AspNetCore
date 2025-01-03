using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.ModelBinding
{
    class ModelBindingUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-bind-body-and-route-data
        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var requestModel = await request.Bind<RequestModel>();
            var body = requestModel.Body;
            var aString = body.AString;
            var id = requestModel.Id;

            //use values as needed
        }
        // end-snippet
    }
    
    class ModelBindingTryBindUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-try-bind
        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var (model, isModelSet, modelState) = await request.TryBind<RequestModel>();
            //use values as needed
        }
        // end-snippet
    }
}