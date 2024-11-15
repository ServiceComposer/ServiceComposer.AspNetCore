using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ServiceComposer.AspNetCore;

namespace Snippets.Events;

// begin-snippet: generic-event-handler
public class GenericEventHandler : ICompositionEventsHandler<AnEvent>
{
    public Task Handle(AnEvent @event, HttpRequest request)
    {
        // handle the event
        return Task.CompletedTask;
    }
}
// end-snippet