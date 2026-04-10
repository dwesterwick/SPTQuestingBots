using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Routers.Internal
{
    internal static class RouteManager
    {
        private static readonly Dictionary<string, IRouteInfo> _allRegisteredRoutes = new();

        public static void RegisterRoutes<T>(IEnumerable<string> routeNames, IRouteHandler handler) where T : class, IRequestData
        {
            foreach (string routeName in routeNames)
            {
                RegisterRoute<T>(routeName, handler);
            }
        }

        public static void RegisterRoute<T>(string routeName, IRouteHandler handler) where T : class, IRequestData
        {
            if (_allRegisteredRoutes.ContainsKey(routeName))
            {
                throw new InvalidOperationException($"Route \"{routeName}\" is already registered");
            }

            TypedRouteInfo<T> routeInfo = new TypedRouteInfo<T>(routeName, handler);
            _allRegisteredRoutes.Add(routeName, routeInfo);
        }

        public static IEnumerable<RouteAction> GetRoutes(IEnumerable<string> routeNames)
        {
            foreach (string _routeName in routeNames)
            {
                RouteAction? routeAction = GetRoute(_routeName);
                if (routeAction == null)
                {
                    continue;
                }

                yield return routeAction;
            }
        }

        public static RouteAction? GetRoute(string routeName)
        {
            if (!_allRegisteredRoutes.TryGetValue(routeName, out IRouteInfo? routeInfo) || routeInfo == null)
            {
                throw new InvalidOperationException($"Cannot retrieve route for \"{routeName}\"");
            }

            return routeInfo.Action;
        }
    }
}
