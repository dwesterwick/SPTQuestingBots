using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.ExternalMods.Functions.Extract
{
    public class SAINExtractFunction : AbstractExtractFunction
    {
        public override string MonitoredLayerName => "SAIN : Extract";

        public SAINExtractFunction(BotOwner _botOwner) : base(_botOwner)
        {

        }

        public override bool IsTryingToExtract() => IsMonitoredLayerActive();

        private bool tryExtractSingleBot(BotOwner botOwner) => SAIN.Plugin.SAINInterop.TryExtractBot(botOwner);
        private bool trySetExfilForBot(BotOwner botOwner) => SAIN.Plugin.SAINInterop.TrySetExfilForBot(botOwner);

        public override bool TryInstructBotToExtract()
        {
            if (!tryExtractSingleBot(BotOwner))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot instruct " + BotOwner.GetText() + " to extract. SAIN Interop not initialized properly or is outdated.");

                return false;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Instructing " + BotOwner.GetText() + " to extract now");

            foreach (BotOwner follower in HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner))
            {
                if ((follower == null) || follower.IsDead)
                {
                    continue;
                }

                if (tryExtractSingleBot(follower))
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Instructing follower " + follower.GetText() + " to extract now");
                }
                else
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Could not instruct follower " + follower.GetText() + " to extract now. SAIN Interop not initialized properly or is outdated.");
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
