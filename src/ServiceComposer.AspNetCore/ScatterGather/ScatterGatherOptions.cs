using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

public class ScatterGatherOptions
{
    public Type CustomAggregator { get; set; }
    internal IAggregator GetAggregator(HttpContext httpContext)
    {
        if(CustomAggregator != null)
        {
            return (IAggregator)httpContext.RequestServices.GetRequiredService(CustomAggregator);
        }
        
        return new DefaultAggregator();
    }

    public IList<IGatherer> Gatherers { get; set; }
}