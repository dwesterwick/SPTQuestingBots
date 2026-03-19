using QuestingBots.Services.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace QuestingBots.Services
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET)]
    public class RemoveBlacklistedBrainTypesService : AbstractService
    {
        private PmcConfig _pmcConfig;
        private BotConfig _botConfig;

        public RemoveBlacklistedBrainTypesService(LoggingUtil logger, ConfigUtil config, ConfigServer configServer) : base(logger, config)
        {
            _pmcConfig = configServer.GetConfig<PmcConfig>();
            _botConfig = configServer.GetConfig<BotConfig>();
        }

        protected override void OnLoadIfModIsEnabled()
        {
            RemoveBlacklistedPMCBrains(Config.CurrentConfig.BotSpawns.BlacklistedPMCBotBrains);
            RemoveBlacklistedPlayerScavBrains(Config.CurrentConfig.BotSpawns.BlacklistedPMCBotBrains);
        }

        private void RemoveBlacklistedPMCBrains(IEnumerable<string> blacklistedbrainTypes)
        {
            int removedBrains = 0;
            foreach (string pmcType in _pmcConfig.PmcType.Keys)
            {
                foreach (string map in _pmcConfig.PmcType[pmcType].Keys)
                {
                    foreach (string blacklistedBrain in blacklistedbrainTypes)
                    {
                        removedBrains += _pmcConfig.PmcType[pmcType][map].Remove(blacklistedBrain) ? 1 : 0;
                    }
                }
            }

            Logger.Info($"Removed {removedBrains} blacklisted PMC brain types");
        }

        private void RemoveBlacklistedPlayerScavBrains(IEnumerable<string> blacklistedbrainTypes)
        {
            int removedBrains = 0;
            foreach (string map in _botConfig.PlayerScavBrainType.Keys)
            {
                foreach (string blacklistedBrain in blacklistedbrainTypes)
                {
                    removedBrains += _botConfig.PlayerScavBrainType[map].Remove(blacklistedBrain) ? 1 : 0;
                }
            }

            Logger.Info($"Removed {removedBrains} blacklisted Player Scav brain types");
        }
    }
}
