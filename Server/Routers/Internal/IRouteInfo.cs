using SPTarkov.Server.Core.DI;

namespace QuestingBots.Routers.Internal
{
    public interface IRouteInfo
    {
        public string Name { get; }
        public IRouteHandler Handler { get; }
        public string Path { get; }
        public RouteAction? Action { get; }
    }
}
