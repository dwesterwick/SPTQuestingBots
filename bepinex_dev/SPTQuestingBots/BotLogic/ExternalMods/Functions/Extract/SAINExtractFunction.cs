using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract
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
                LoggingController.LogWarning("Cannot instruct " + BotOwner.GetText() + " to extract. SAIN Interop not initialized properly or is outdated.");

                return false;
            }

            LoggingController.LogDebug("Instructing " + BotOwner.GetText() + " to extract now");

            foreach (BotOwner follower in HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner))
            {
                if ((follower == null) || follower.IsDead)
                {
                    continue;
                }

                if (tryExtractSingleBot(follower))
                {
                    LoggingController.LogDebug("Instructing follower " + follower.GetText() + " to extract now");
                }
                else
                {
                    LoggingController.LogWarning("Could not instruct follower " + follower.GetText() + " to extract now. SAIN Interop not initialized properly or is outdated.");
                }
            }

            if (!trySetExfilForBot(BotOwner))
            {
                LoggingController.LogWarning("Could not find an extract for " + BotOwner.GetText());
                return false;
            }

            return true;
        }
    }
}
