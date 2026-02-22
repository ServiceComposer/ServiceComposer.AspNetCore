using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceComposer.AspNetCore;

public class HttpGatherer : Gatherer<JsonNode>
{
    public HttpGatherer(string key, string destinationUrl)
        : base(key)
    {
        DestinationUrl = destinationUrl;

        DefaultDestinationUrlMapper = MapDestinationUrl;
        DestinationUrlMapper = (request, destination) => DefaultDestinationUrlMapper(request, destination);

        DefaultHeadersMapper = MapHeaders;
        HeadersMapper = (request, requestMessage) => DefaultHeadersMapper(request, requestMessage);
    }

    public string DestinationUrl { get; }

    public Func<HttpRequest, string, string> DefaultDestinationUrlMapper { get; }

    public Func<HttpRequest, string, string> DestinationUrlMapper { get; init; }

    /// <summary>
    /// Controls whether incoming request headers are forwarded to the downstream destination.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool ForwardHeaders { get; init; } = true;

    /// <summary>
    /// The default header-forwarding implementation. Forwards all incoming request headers
    /// to the outgoing <see cref="HttpRequestMessage"/> when <see cref="ForwardHeaders"/> is
    /// <c>true</c>. Assign <see cref="HeadersMapper"/> to customize, filter, add or remove headers.
    /// </summary>
    public Action<HttpRequest, HttpRequestMessage> DefaultHeadersMapper { get; }

    /// <summary>
    /// Delegate applied to the outgoing <see cref="HttpRequestMessage"/> before the downstream
    /// request is sent. Replace this to customize, filter, add or remove headers.
    /// Defaults to calling <see cref="DefaultHeadersMapper"/>.
    /// </summary>
    public Action<HttpRequest, HttpRequestMessage> HeadersMapper { get; init; }

    protected virtual string MapDestinationUrl(HttpRequest request, string destination)
    {
        return request.Query.Count == 0
            ? destination
            : $"{destination}{request.QueryString}";
    }

    protected virtual void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
    {
        if (!ForwardHeaders)
        {
            return;
        }

        foreach (var header in request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }
    }

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

    public override async Task<IEnumerable<JsonNode>> Gather(HttpContext context)
    {
        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(Key);
        var destination = DestinationUrlMapper(context.Request, DestinationUrl);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, destination);
        HeadersMapper(context.Request, requestMessage);
        var response = await client.SendAsync(requestMessage);
        return await TransformResponse(response);
    }
}
