using System.Diagnostics;

namespace ServiceComposer.AspNetCore
{
    static class CompositionTelemetry
    {
        internal static readonly ActivitySource ActivitySource = new("ServiceComposer.AspNetCore.ViewModelComposition", "0.1.0");
        internal static readonly ActivitySource ScatterGatherActivitySource = new("ServiceComposer.AspNetCore.ScatterGather", "0.1.0");
    }
}
