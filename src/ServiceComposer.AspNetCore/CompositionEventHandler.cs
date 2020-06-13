using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public delegate Task CompositionEventHandler<TEvent>(TEvent @event, HttpRequest httpRequest);
}