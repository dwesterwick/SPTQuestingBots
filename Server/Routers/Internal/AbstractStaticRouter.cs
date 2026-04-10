using QuestingBots.Utils;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers.Internal
{
    internal abstract class AbstractStaticRouter : AbstractTypedStaticRouter<EmptyRequestData>
    {
        public AbstractStaticRouter(IEnumerable<string> _routeNames, LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil)
            : base(_routeNames, logger, config, jsonUtil)
        {

        }
    }
}
