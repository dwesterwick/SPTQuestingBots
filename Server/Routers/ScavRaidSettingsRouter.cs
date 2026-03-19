using QuestingBots.Helpers;
using QuestingBots.Routers.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers
{
    [Injectable]
    internal class ScavRaidSettingsRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetScavRaidSettings"];

        private LocationConfig _locationConfig;

        public ScavRaidSettingsRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, ConfigServer configServer) : base(_routeNames, logger, config, jsonUtil)
        {
            _locationConfig = configServer.GetConfig<LocationConfig>();
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(_locationConfig.ScavRaidTimeSettings.Maps);
            return new ValueTask<string?>(json);
        }
    }
}
