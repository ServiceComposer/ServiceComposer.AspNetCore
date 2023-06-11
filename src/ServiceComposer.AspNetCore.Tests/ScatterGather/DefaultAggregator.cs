using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ServiceComposer.AspNetCore.Tests.ScatterGather;

class DefaultAggregator : IAggregator
{
    readonly List<HttpResponseMessage> _responseMessages = new();

    public void Add(HttpResponseMessage response)
    {
        _responseMessages.Add(response);
    }

    public async Task<string> Aggregate()
    {
        var responsesArray = new JsonArray();
        foreach (var responseMessage in _responseMessages)
        {
            var gathererResponsesAsString = await responseMessage.Content.ReadAsStringAsync();
            // default behavior assumes downstream service returns a JSON array
            var gathererResponses = JsonNode.Parse(gathererResponsesAsString)?.AsArray();
            if (gathererResponses is { Count: > 0 })
            {
                // TODO: this has the side effect of reversing the order of the responses
                for (var i = gathererResponses.Count - 1; i >= 0; i--)
                {
                    var nodeAtIndex = gathererResponses[i];
                    gathererResponses.Remove(nodeAtIndex);
                    responsesArray.Add(nodeAtIndex);
                }
            }
        }

        return responsesArray.ToJsonString();
    }
}