using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using ServiceComposer.AspNetCore;

namespace Snippets.EndpointFilters;

static class ComponentsConvention
{
    static void Show()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddViewModelComposition();
        var app = builder.Build();

        // begin-snippet: composition-endpoint-components
        app.MapCompositionHandlers().Add(endpointBuilder =>
        {
            var components = endpointBuilder.Metadata
                .OfType<CompositionEndpointComponents>()
                .Single()
                .Components;

            foreach (var component in components.Where(c => c.Kind == CompositionComponentKind.RequestsHandler))
            {
                // component.UserType and component.Method are the user's class and method,
                // also for contract-less composition handlers.
                Console.WriteLine($"{endpointBuilder.DisplayName}: {component.UserType.Name}.{component.Method.Name}, " +
                                  $"{component.TypeAttributes.Count} class and {component.MethodAttributes.Count} method attributes");
            }
        });
        // end-snippet
    }
}
