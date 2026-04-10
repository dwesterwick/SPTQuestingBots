namespace QuestingBots.Routers.Internal
{
    public interface IRouteHandler
    {
        public bool ShouldCreateRoutes();
        public bool ShouldHandleRoutes();
        public ValueTask<string?> HandleRoute(string routeName, RequestData routerData);
    }
}
