using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class Gatherer
{
    public Gatherer()
    {
        DefaultDestinationUrlMapper= request => request.Query.Count == 0
            ? Destination
            : $"{Destination}{request.QueryString}";
        
        DestinationUrlMapper = request => DefaultDestinationUrlMapper(request);
    }
    
    public string Key { get; init; }
    public string Destination { get; init; }
    
    public Func<HttpRequest, string> DefaultDestinationUrlMapper { get; }
    
    public Func<HttpRequest, string> DestinationUrlMapper { get; init; }
}