using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore
{
        [Obsolete(message:"EventHandler<TEvent> is obsoleted and will be treated as an error starting v2 and removed in v3. Use attribute routing based composition, and CompositionEventHandler<TEvent>.", error:false)]
        public delegate Task EventHandler<TEvent>(string requestId, dynamic viewModel, TEvent @event, RouteData routeData, HttpRequest httpRequest);
}