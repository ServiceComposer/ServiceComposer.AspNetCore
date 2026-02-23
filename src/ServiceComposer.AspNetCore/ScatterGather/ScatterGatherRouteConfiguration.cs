using System.Collections.Generic;

namespace ServiceComposer.AspNetCore;

/// <summary>
/// Represents the configuration for a single scatter/gather route as loaded from an
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> source (e.g.
/// <c>appsettings.json</c>).
/// </summary>
public class ScatterGatherRouteConfiguration
{
    /// <summary>
    /// The route template, e.g. <c>"api/products"</c>.
    /// </summary>
    public string Template { get; set; }

    /// <summary>
    /// When <c>true</c>, the MVC output formatter pipeline is used for content negotiation.
    /// Corresponds to <see cref="ScatterGatherOptions.UseOutputFormatters"/>.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool UseOutputFormatters { get; set; }

    /// <summary>
    /// The gatherers for this route. Each entry maps to an <see cref="HttpGatherer"/>.
    /// </summary>
    public IList<HttpGathererConfiguration> Gatherers { get; set; } = [];
}
