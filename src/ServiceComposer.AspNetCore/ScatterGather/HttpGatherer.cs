using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

public class HttpGatherer(string key, string destinationUrl) : Gatherer<JsonNode>(key)
{
    public string DestinationUrl { get; } = destinationUrl;

    public static Func<HttpRequest, string, string> DefaultDestinationUrlMapper { get; } = (request, destination) => request.Query.Count == 0
        ? destination
        : $"{destination}{request.QueryString}";

    public Func<HttpRequest, string, string> DestinationUrlMapper { get; set; } = DefaultDestinationUrlMapper;

    /// <summary>
    /// Controls whether incoming request headers are forwarded to the downstream destination.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool ForwardHeaders { get; set; } = true;

    /// <summary>
    /// The default header-forwarding implementation. Forwards all incoming request headers
    /// to the outgoing <see cref="HttpRequestMessage"/> when <see cref="ForwardHeaders"/> is
    /// <c>true</c>. Assign <see cref="HeadersMapper"/> to customize, filter, add or remove headers.
    /// </summary>
    public static Action<HttpRequest, HttpRequestMessage> DefaultHeadersMapper { get; } = (incomingHttpRequest, outgoingRequestMessage) =>
    {
        foreach (var header in incomingHttpRequest.Headers)
        {
            outgoingRequestMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }
    };

    /// <summary>
    /// Delegate applied to the outgoing <see cref="HttpRequestMessage"/> before the downstream
    /// request is sent. Replace this to customize, filter, add or remove headers.
    /// Defaults to calling <see cref="DefaultHeadersMapper"/>.
    /// </summary>
    public Action<HttpRequest, HttpRequestMessage> HeadersMapper { get; set; } = DefaultHeadersMapper;

    protected virtual string MapDestinationUrl(HttpRequest request, string destination) => DestinationUrlMapper(request, destination);

    protected virtual void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        if (!ForwardHeaders)
        {
            return;
        }

        HeadersMapper(request, requestMessage);
    }

    protected virtual async Task<IEnumerable<JsonNode>> TransformResponse(HttpResponseMessage responseMessage)
    {
        var gathererResponsesAsString = await responseMessage.Content.ReadAsStringAsync();
        // default behavior assumes downstream service returns a JSON array
        var gathererResponses = JsonNode.Parse(gathererResponsesAsString)?.AsArray();
        if (gathererResponses is not { Count: > 0 })
        {
            return [];
        }
        
        var nodes = new JsonNode[gathererResponses.Count];
        for (var i = gathererResponses.Count - 1; i >= 0; i--)
        {
            var nodeAtIndex = gathererResponses[i];
            gathererResponses.Remove(nodeAtIndex);
            nodes[i] = nodeAtIndex;
        }
            
        return nodes;

    }

    public override async Task<IEnumerable<JsonNode>> Gather(HttpContext context)
    {
        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(Key);
        
        var destination = MapDestinationUrl(context.Request, DestinationUrl);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, destination);
        MapHeaders(context.Request, requestMessage);
        
        var response = await client.SendAsync(requestMessage);
        
        return await TransformResponse(response);
    }
}
