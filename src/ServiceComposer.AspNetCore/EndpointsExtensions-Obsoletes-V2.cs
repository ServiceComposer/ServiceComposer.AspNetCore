using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ServiceComposer.AspNetCore;

static partial class EndpointsExtensions
{
    [Obsolete(
        "To enable write support use the EnableWriteSupport() method on the ViewModelCompositionOptions. This method will be treated as an error in v2 and removed in v3.", error: true)]
    public static IEndpointConventionBuilder MapCompositionHandlers(this IEndpointRouteBuilder endpoints, bool enableWriteSupport)
    {
        throw new Exception();
    }
}