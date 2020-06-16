using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.HandlerPublishEvent
{
    // begin-snippet: net-core-3x-handler-publish-event
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            var vm = request.GetComposedResponseModel();
            await vm.RaiseEvent(new SampleEvent() {SomeValue = 42});
        }
    }

    public class SampleEvent
    {
        public int SomeValue { get; set; }
    }
    // end-snippet
}