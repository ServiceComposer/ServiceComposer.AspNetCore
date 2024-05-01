using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.ModelBinding
{
    // begin-snippet: model-binding-model
    class BodyModel
    {
        public string AString { get; set; }
    }
    // end-snippet

    // begin-snippet: model-binding-request
    class RequestModel
    {
        [FromRoute] public int id { get; set; }
        [FromBody] public BodyModel Body { get; set; }
    }
    // end-snippet

    class ModelBindingUsageHandler : ICompositionRequestsHandler
    {
        // begin-snippet: model-binding-bind-body-and-route-data
        [HttpPost("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var requestModel = await request.Bind<RequestModel>();
            var body = requestModel.Body;
            var aString = body.AString;
            var id = requestModel.id;

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