using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    class CompositionOverControllersRoutes
    {
        readonly Type[] empty = Type.EmptyTypes;
        readonly Dictionary<string, Dictionary<string, Type[]>> routes = new();

        public void AddGetComponentsSource(Dictionary<string, Type[]> compositionOverControllerGetComponents)
        {
            routes["get"] = compositionOverControllerGetComponents ?? throw new ArgumentNullException(nameof(compositionOverControllerGetComponents));
        }

        public void AddPostComponentsSource(Dictionary<string, Type[]> compositionOverControllerPostComponents)
        {
            routes["post"] = compositionOverControllerPostComponents ?? throw new ArgumentNullException(nameof(compositionOverControllerPostComponents));
        }

        public Type[] HandlersForRoute(string routePatternRawText, string requestMethod)
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