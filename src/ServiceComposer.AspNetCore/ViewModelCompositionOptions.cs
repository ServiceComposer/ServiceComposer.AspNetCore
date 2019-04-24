using System;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; private set; }
        public bool IsAssemblyScanningDisabled { get; private set; }
        public void DisableAssemblyScanning()
        {
            IsAssemblyScanningDisabled = true;
        }

        public void RegisterRequestsHandler<T>() where T: IHandleRequests
        {
            RegisterRouteInterceptor(typeof(T));
        }

        internal void RegisterRouteInterceptor(Type type)
        {
            Services.AddSingleton(typeof(IInterceptRoutes), type);
        }
    }
}