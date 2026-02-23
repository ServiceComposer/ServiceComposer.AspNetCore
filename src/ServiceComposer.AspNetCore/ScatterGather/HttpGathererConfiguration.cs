namespace ServiceComposer.AspNetCore;

/// <summary>
/// Represents the configuration for a single <see cref="HttpGatherer"/> as loaded from
/// an <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> source (e.g.
/// <c>appsettings.json</c>).
/// </summary>
public class HttpGathererConfiguration
{
    /// <summary>
    /// The key that uniquely identifies this gatherer within a route.
    /// Maps to the <see cref="HttpGatherer"/> <c>key</c> constructor argument.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The downstream URL to invoke.
    /// Maps to the <see cref="HttpGatherer"/> <c>destinationUrl</c> constructor argument.
    /// </summary>
    public string DestinationUrl { get; set; }
}
