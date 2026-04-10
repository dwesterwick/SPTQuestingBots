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
    internal class USECChanceRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetUSECChance"];

        private PmcConfig _pmcConfig;

        public USECChanceRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, ConfigServer configServer) : base(_routeNames, logger, config, jsonUtil)
        {
            _pmcConfig = configServer.GetConfig<PmcConfig>();
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            Configuration.ServerResponses.ServerResponse response = new Configuration.ServerResponses.ServerResponse(_pmcConfig.IsUsec);
            string json = ConfigHelpers.Serialize(response);
            return new ValueTask<string?>(json);
        }
    }
}
