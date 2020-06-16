using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.SampleSubscriber
{
    // begin-snippet: net-core-3x-sample-subscriber
    public class SampleEvent
    {
        
    }
    
    public class SampleSubscriber: ICompositionEventsSubscriber
    {
        [HttpGet("/sample/{id}")]
        public void Subscribe(ICompositionEventsPublisher publisher)
        {
            publisher.Subscribe<SampleEvent>((@event, request) =>
            {
                return Task.CompletedTask;
            });
        }
    }
    // end-snippet
}