using System.Diagnostics;

namespace ServiceComposer.AspNetCore
{
    static class CompositionTelemetry
    {
        internal static readonly ActivitySource ActivitySource = new("ServiceComposer.AspNetCore.ViewModelComposition");
        internal static readonly ActivitySource ScatterGatherActivitySource = new("ServiceComposer.AspNetCore.ScatterGather");
    }
}
