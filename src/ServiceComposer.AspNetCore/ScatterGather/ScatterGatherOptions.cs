using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace ServiceComposer.AspNetCore;

public class ScatterGatherOptions
{
    public Type CustomAggregator
    {
        get;
        set
        {
            if (value != null && !typeof(IAggregator).IsAssignableFrom(value))
            {
                throw new InvalidOperationException(
                    $"CustomAggregator type '{value.FullName}' does not implement {nameof(IAggregator)}.");
            }

            field = value;
        }
    }

    /// <summary>
    /// Configures scatter/gather to use the MVC defined output formatters for content negotiation.
    /// When enabled, the response format (e.g. JSON, XML) is chosen based on the incoming
    /// <c>Accept</c> header. Gatherers should return plain .NET objects rather than
    /// <see cref="System.Text.Json.Nodes.JsonNode"/> values so that all formatters can serialize them.
    /// To use output formatters MVC services must be configured, e.g. via
    /// <c>services.AddControllers()</c>.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool UseOutputFormatters { get; set; }

    internal IAggregator GetAggregator(HttpContext httpContext)
    {
        if (CustomAggregator != null)
        {
            return (IAggregator)httpContext.RequestServices.GetService(CustomAggregator)
                   ?? throw new InvalidOperationException($"The requested custom aggregator {CustomAggregator} cannot be resolved. Is it registered in the ServiceCollection?.");
        }

        return UseOutputFormatters ? new DefaultObjectAggregator() : new DefaultAggregator();
    }

    public IList<IGatherer> Gatherers { get; set; } = [];
}
