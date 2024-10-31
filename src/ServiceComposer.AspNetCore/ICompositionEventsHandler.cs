using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public interface ICompositionEventsHandler<in TEvent>
{
    Task Handle(TEvent @event, HttpRequest request);
}