using SPTarkov.Server.Core.Models.Eft.Common;

namespace QuestingBots.Routers.Internal
{
    internal class RouteInfo : TypedRouteInfo<EmptyRequestData>
    {
        public RouteInfo(string routeName, IRouteHandler routerInstance) : base(routeName, routerInstance)
        {

        }
    }
}