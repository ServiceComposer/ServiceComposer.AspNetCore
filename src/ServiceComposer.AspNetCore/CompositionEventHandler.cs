using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore
{
    public delegate Task CompositionEventHandler<in TEvent>(TEvent @event, HttpRequest httpRequest);
}