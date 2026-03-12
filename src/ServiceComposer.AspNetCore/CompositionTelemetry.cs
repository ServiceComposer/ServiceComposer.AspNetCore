using System.Diagnostics;

namespace ServiceComposer.AspNetCore
{
    public static class CompositionTelemetry
    {
        public const string ViewModelCompositionSourceName = "ServiceComposer.AspNetCore.ViewModelComposition";
        public const string ScatterGatherSourceName = "ServiceComposer.AspNetCore.ScatterGather";

        public static class Spans
        {
            public const string Handler = "composition.handler";
            public const string Event = "composition.event";
            public const string Gatherer = "scatter-gather.gatherer";
        }

        public static class Tags
        {
            public const string HandlerType = "composition.handler.type";
            public const string HandlerNamespace = "composition.handler.namespace";
            public const string EventType = "composition.event.type";
            public const string EventNamespace = "composition.event.namespace";
            public const string GathererKey = "scatter_gather.gatherer.key";
        }

        internal static readonly ActivitySource ActivitySource = new(ViewModelCompositionSourceName, "0.1.0");
        internal static readonly ActivitySource ScatterGatherActivitySource = new(ScatterGatherSourceName, "0.1.0");
    }
}
