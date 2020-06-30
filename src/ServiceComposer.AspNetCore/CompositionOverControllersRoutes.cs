using System;
using System.Collections.Generic;

namespace ServiceComposer.AspNetCore
{
    internal class CompositionOverControllersRoutes
    {
        private readonly Type[] empty = new Type[0];
        private Dictionary<string, Type[]> _compositionOverControllerGetComponents;
        private Dictionary<string, Type[]> _compositionOverControllerPostComponents;

        public void AddGetComponentsSource(Dictionary<string, Type[]> compositionOverControllerGetComponents)
        {
            _compositionOverControllerGetComponents = compositionOverControllerGetComponents ?? throw new ArgumentNullException(nameof(compositionOverControllerGetComponents));
        }

        public void AddPostComponentsSource(Dictionary<string, Type[]> compositionOverControllerPostComponents)
        {
            _compositionOverControllerPostComponents = compositionOverControllerPostComponents ?? throw new ArgumentNullException(nameof(compositionOverControllerPostComponents));
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
                case "post":
                    if (_compositionOverControllerPostComponents.ContainsKey(routePatternRawText))
                    {
                        return _compositionOverControllerPostComponents[routePatternRawText];
                    }
                    break;
            }

            return empty;
        }
    }
}