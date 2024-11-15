using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.Events;

// begin-snippet: publishing-events
public class EventPublishingHandler : ICompositionRequestsHandler
{
    [HttpGet("/route-based-handler/{some-id}")]
    public async Task Handle(HttpRequest request)
    {
        var context = request.GetCompositionContext();
        await context.RaiseEvent(new AnEvent(SomeValue: "This is the value"));
    }
}
// end-snippet