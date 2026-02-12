using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersRoutes
    {
        static readonly (Type ComponentType, IList<object> Metadata)[] empty = [];
        readonly Dictionary<string, Dictionary<string, (Type ComponentType, IList<object> Metadata)[]>> routes = new();

        public void AddComponentsSource(string httpMethod, Dictionary<string, (Type ComponentType, IList<object> Metadata)[]> components)
        {
            routes[httpMethod.ToLowerInvariant()] = components ?? throw new ArgumentNullException(nameof(components));
        }

        public (Type ComponentType, IList<object> Metadata)[] HandlersForRoute(string routePatternRawText, string requestMethod)
        {
            var results = empty;
            requestMethod = requestMethod.ToLowerInvariant();
            if (routes.ContainsKey(requestMethod))
            {
                var methodRoutes = routes[requestMethod];
                if (methodRoutes.ContainsKey(routePatternRawText))
                {
                    results = methodRoutes[routePatternRawText];
                }
            }

            return results;
        }
    }
}
