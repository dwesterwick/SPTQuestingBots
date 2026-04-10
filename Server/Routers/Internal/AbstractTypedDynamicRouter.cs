using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers.Internal;

internal abstract class AbstractTypedDynamicRouter<T> : DynamicRouter, IRouteHandler where T : class, IRequestData
{
    protected static ConfigUtil Config { get; private set; } = null!;

    protected LoggingUtil Logger { get; private set; } = null!;
    protected JsonUtil JsonUtil { get; private set; } = null!;

    public AbstractTypedDynamicRouter(IEnumerable<string> _routeNames, LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil) : base(jsonUtil, RouteManager.GetRoutes(_routeNames))
    {
        if (Config == null)
        {
            Config = config;
        }

        Logger = logger;
        JsonUtil = jsonUtil;

        RouteManager.RegisterRoutes<T>(_routeNames, this);
    }

    public virtual bool ShouldCreateRoutes() => Config.CurrentConfig.IsModEnabled();
    public virtual bool ShouldHandleRoutes() => true;

    public abstract ValueTask<string?> HandleRoute(string routeName, RequestData routerData);
}