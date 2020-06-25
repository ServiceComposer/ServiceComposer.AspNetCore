using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    internal class CompositionOverControllersRoutes
    {
        private Dictionary<string, Type[]> _compositionOverControllerGetComponents;
        private readonly Type[] empty = new Type[0];
        public void AddGetComponentsSource(Dictionary<string, Type[]> compositionOverControllerGetComponents)
        {
            _compositionOverControllerGetComponents = compositionOverControllerGetComponents ?? throw new ArgumentNullException(nameof(compositionOverControllerGetComponents));
        }

        public Type[] HandlersForRoute(string routePatternRawText, string requestMethod)
        {
            switch (requestMethod.ToLowerInvariant())
            {
                case "get":
                    if (_compositionOverControllerGetComponents.ContainsKey(routePatternRawText))
                    {
                        return _compositionOverControllerGetComponents[routePatternRawText];
                    }
                    break;
            }
            
            return empty;
        }
    }
}