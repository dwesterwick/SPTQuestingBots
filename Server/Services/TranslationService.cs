using QuestingBots.Services.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace QuestingBots.Services
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET)]
    public class TranslationService : AbstractService
    {
        private ServerLocalisationService _serverLocalisationService;

        private Dictionary<string, string> _cachedTranslations = new();

        public TranslationService(LoggingUtil logger, ConfigUtil config, ServerLocalisationService serverLocalisationService) : base(logger, config)
        {
            _serverLocalisationService = serverLocalisationService;
        }

        protected override void OnLoadIfModIsEnabled()
        {
            
        }

        public virtual string GetLocalisedValue(string key)
        {
            if (_cachedTranslations.ContainsKey(key))
            {
                return _cachedTranslations[key];
            }

            string translation = _serverLocalisationService.GetLocalisedValue(key);
            _cachedTranslations.Add(key, translation);

            return translation;
        }

        public string GetLocalisedTraderName(Trader trader) => GetLocalisedValue($"{trader.Base.Id} Nickname");
        public string GetLocalisedItemName(TemplateItem item) => GetLocalisedValue($"{item.Id} Name");
    }
}
