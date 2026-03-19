using QuestingBots.Helpers;
using QuestingBots.Routers.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers
{
    [Injectable]
    internal class ZoneAndItemPositionsRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetZoneAndItemQuestPositions"];

        public ZoneAndItemPositionsRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil) : base(_routeNames, logger, config, jsonUtil)
        {

        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(Config.ZoneAndItemQuestPositions);
            return new ValueTask<string?>(json);
        }
    }
}
