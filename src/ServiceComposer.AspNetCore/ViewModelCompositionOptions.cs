using System;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore
{
    public class ViewModelCompositionOptions
    {
        private IServiceCollection services;

        internal ViewModelCompositionOptions(IServiceCollection services)
        {
            this.services = services;
        }

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
            services.AddSingleton(typeof(IInterceptRoutes), type);
        }
    }
}