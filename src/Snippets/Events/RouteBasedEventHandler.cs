using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.Events;

// begin-snippet: route-based-event-handler
public class RouteBasedEventHandler : ICompositionEventsSubscriber
{
    [HttpGet("/route-based-handler/{some-id}")]
    public void Subscribe(ICompositionEventsPublisher publisher)
    {
        publisher.Subscribe<AnEvent>((@event, request) =>
        {
            // handle the event
            return Task.CompletedTask;
        });
    }
}
// end-snippet