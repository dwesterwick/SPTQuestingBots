using Comfort.Common;
using EFT;
using QuestingBots.ExternalMods.Interop;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.ExternalMods.Functions.Extract
{
    public class SAINExtractFunction : AbstractExtractFunction
    {
        public override string MonitoredLayerName => "SAIN : Extract";

        public SAINExtractFunction(BotOwner _botOwner) : base(_botOwner)
        {

        }

        public override bool IsTryingToExtract() => IsMonitoredLayerActive();

        private bool tryExtractSingleBot(BotOwner botOwner) => SAINInterop.TryExtractBot(botOwner);
        private bool trySetExfilForBot(BotOwner botOwner) => SAINInterop.TrySetExfilForBot(botOwner);

        public override bool TryInstructBotToExtract()
        {
            if (!tryExtractSingleBot(BotOwner))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot instruct " + BotOwner.GetText() + " to extract. SAIN Interop not initialized properly or is outdated.");
                return false;
            }

            if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Instructing " + BotOwner.GetText() + " to extract now");
            }

            foreach (BotOwner follower in BotLogic.HiveMind.BotHiveMindMonitor.GetGroupFollowers(BotOwner))
            {
                if ((follower == null) || follower.IsDead)
                {
                    continue;
                }

                if (!tryExtractSingleBot(follower))
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Could not instruct follower " + follower.GetText() + " to extract now. SAIN Interop not initialized properly or is outdated.");
                    continue;
                }

                if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Instructing follower " + follower.GetText() + " to extract now");
                }

                if (!trySetExfilForBot(follower))
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Could not find an extract for " + follower.GetText());
                }
            }

            if (!trySetExfilForBot(BotOwner))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not find an extract for " + BotOwner.GetText());
                return false;
            }

            return true;
        }
    }
}
