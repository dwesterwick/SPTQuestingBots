using QuestingBots.Helpers;
using QuestingBots.Routers.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers
{
    [Injectable]
    internal class CarExtractNamesRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetCarExtractNames"];

        private InRaidConfig _inRaidConfig;

        public CarExtractNamesRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, InRaidConfig inRaidConfig) : base(_routeNames, logger, config, jsonUtil)
        {
            _inRaidConfig = inRaidConfig;
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(_inRaidConfig.CarExtracts);
            return new ValueTask<string?>(json);
        }
    }
}
