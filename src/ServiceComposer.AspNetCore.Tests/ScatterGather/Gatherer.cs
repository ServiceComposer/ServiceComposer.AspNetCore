using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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

    protected virtual async Task<IEnumerable<JsonNode>> TransformResponse(HttpResponseMessage responseMessage)
    {
        var nodes = new List<JsonNode>();
        var gathererResponsesAsString = await responseMessage.Content.ReadAsStringAsync();
        // default behavior assumes downstream service returns a JSON array
        var gathererResponses = JsonNode.Parse(gathererResponsesAsString)?.AsArray();
        if (gathererResponses is { Count: > 0 })
        {
            // this has the side effect of reversing the order
            // of the responses. This is why we reverse below.
            for (var i = gathererResponses.Count - 1; i >= 0; i--)
            {
                var nodeAtIndex = gathererResponses[i];
                gathererResponses.Remove(nodeAtIndex);
                nodes.Add(nodeAtIndex);
            }
            nodes.Reverse();
        }

        return nodes;
    }

    public virtual async Task<IEnumerable<JsonNode>> Gather(HttpContext context)
    {
        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(Key);
        var destination = DestinationUrlMapper(context.Request);
        var response = await client.GetAsync(destination);
        return await TransformResponse(response);
    }
}