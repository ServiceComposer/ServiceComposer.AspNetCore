using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceComposer.AspNetCore;

namespace TestClassLibraryWithHandlers;

class TestWithEventRequestsHandler : ICompositionRequestsHandler
{
    [HttpGet("/raise-event/{id}")]
    [BindFromRoute<int>("id")]
    public Task Handle(HttpRequest request)
    {
        var compositionContext = request.GetCompositionContext();
#pragma warning disable SC0001
        var id = compositionContext.GetArguments(this).Argument<int>();
#pragma warning restore SC0001
        
        var model = request.GetComposedResponseModel();
        model.Id = id;

        compositionContext.RaiseEvent(new AnEvent() { TheId = id });
        
        return Task.CompletedTask;
    }
}

class AnEvent
{
    public int TheId { get; init; }
}

class TheEventHandler :  ICompositionEventsHandler<AnEvent>
{
    public Task Handle(AnEvent @event, HttpRequest request)
    {
        var model = request.GetComposedResponseModel();
        model.TheIdFromTheEvent = @event.TheId;
        
        return Task.CompletedTask;
    }
}

public class TestModelWithEvent
{
    public int Id { get; set; }
    public int TheIdFromTheEvent { get; set; }
}