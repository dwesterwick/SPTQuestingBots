using QuestingBots.Helpers;
using QuestingBots.Routers.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers
{
    [Injectable]
    internal class QuestTemplatesRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetAllQuestTemplates"];

        private QuestHelper _questHelper;

        public QuestTemplatesRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, QuestHelper questHelper) : base(_routeNames, logger, config, jsonUtil)
        {
            _questHelper = questHelper;
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            string json = ConfigHelpers.Serialize(_questHelper.GetQuestsFromDb());
            return new ValueTask<string?>(json);
        }
    }
}
