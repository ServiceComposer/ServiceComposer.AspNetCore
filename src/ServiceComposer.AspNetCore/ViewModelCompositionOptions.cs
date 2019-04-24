using System;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
            AssemblyScanner = new AssemblyScanner();
        }

        public AssemblyScanner AssemblyScanner { get; private set; }

        public IServiceCollection Services { get; private set; }

        public void RegisterRequestsHandler<T>() where T: IHandleRequests
        {
            RegisterRouteInterceptor(typeof(T));
        }

        public void RegisterCompositionEventsSubscriber<T>() where T : ISubscribeToCompositionEvents
        {
            RegisterRouteInterceptor(typeof(T));
        }

        internal void RegisterRouteInterceptor(Type type)
        {
            Services.AddSingleton(typeof(IInterceptRoutes), type);
        }
    }
}