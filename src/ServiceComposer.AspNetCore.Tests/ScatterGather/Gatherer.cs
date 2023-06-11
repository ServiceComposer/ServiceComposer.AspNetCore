using System;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

public class Gatherer
{
    public Gatherer()
    {
        DestinationUrlMapper = request => request.Query.Count == 0 
            ? Destination 
            : $"{Destination}?{request.QueryString}";
    }
    
    public string Key { get; init; }
    public string Destination { get; init; }
    
    public Func<HttpRequest, string> DestinationUrlMapper { get; init; }
}