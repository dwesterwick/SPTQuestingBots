using QuestingBots.Routers.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers
{
    [Injectable]
    internal class QuestTemplatesRouter : AbstractStaticRouter
    {
        private static readonly string[] _routeNames = ["GetAllQuestTemplates"];

        private JsonUtil _jsonUtil;
        private QuestHelper _questHelper;

        public QuestTemplatesRouter(LoggingUtil logger, ConfigUtil config, JsonUtil jsonUtil, QuestHelper questHelper) : base(_routeNames, logger, config, jsonUtil)
        {
            _jsonUtil = jsonUtil;
            _questHelper = questHelper;
        }

        public override ValueTask<string?> HandleRoute(string routeName, RequestData routerData)
        {
            List<Quest> allQuests = _questHelper.GetQuestsFromDb();
            string? json = _jsonUtil.Serialize(allQuests);
            if (json == null)
            {
                throw new InvalidOperationException("Could not serialize quest templates");
            }

            return new ValueTask<string?>(json);
        }
    }
}
