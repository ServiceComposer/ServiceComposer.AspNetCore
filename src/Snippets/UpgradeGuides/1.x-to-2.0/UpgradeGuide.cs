using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceComposer.AspNetCore;

namespace Snippets.NetCore3x.UpgradeGuides._1.x_to_2._0;

public class UpgradeGuide
{
    public class RunCompositionGatewayDeprecation
    {
        // begin-snippet: run-composition-gateway-deprecation
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseEndpoints(builder => builder.MapCompositionHandlers());
        }
        // end-snippet
    }
    
    public class CompositionOverControllers
    {
        // begin-snippet: composition-over-controllers-case-sensitive
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddViewModelComposition(config =>
            {
                config.EnableCompositionOverControllers(useCaseInsensitiveRouteMatching: false);
            });
        }
        // end-snippet
    }
    
    // begin-snippet: composition-handler-api
    public class SampleHandler : ICompositionRequestsHandler
    {
        [HttpGet("/sample/{id}")]
        public Task Handle(HttpRequest request)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
    
    class SampleEvent{}
    
    // begin-snippet: composition-event-subscriber-api
    public class SamplePublisher : ICompositionEventsSubscriber
    {
        [HttpGet("/sample/{id}")]
        public void Subscribe(ICompositionEventsPublisher publisher)
        {
            // Use the publisher to subscriber to published events
            publisher.Subscribe<SampleEvent>((evt, httpRequest)=>
            {
                // Handle the event
                return Task.CompletedTask;
            });
        }
    }
    // end-snippet
    
    // begin-snippet: composition-errors-handler-api
    public class SampleErrorHandler : ICompositionErrorsHandler
    {
        public Task OnRequestError(HttpRequest request, Exception ex)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
    
    // begin-snippet: viewmodel-preview-handler-api
    public class ViewModelPreviewHandler: IViewModelPreviewHandler
    {
        public Task Preview(HttpRequest httpRequest)
        {
            return Task.CompletedTask;
        }
    }
    // end-snippet
    
    public class CompositionContextApi : ICompositionRequestsHandler
    {
        class AnEvent{}
        
        [HttpGet("/sample/{id}")]
        public async Task Handle(HttpRequest request)
        {
            // begin-snippet: composition-context-api-get-context
            var context = request.GetCompositionContext();
            // end-snippet

            // begin-snippet: composition-context-api-raise-event
            await context.RaiseEvent(new AnEvent());
            // end-snippet
            
            // begin-snippet: composition-context-api-get-request-id
            var requestId = context.RequestId;
            // end-snippet
        }
    }
}