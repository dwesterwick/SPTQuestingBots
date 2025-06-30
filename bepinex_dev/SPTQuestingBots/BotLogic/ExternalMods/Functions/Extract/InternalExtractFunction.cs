using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract
{
    public class InternalExtractFunction : AbstractExtractFunction
    {
        public override string MonitoredLayerName => "Exfiltration";

        public InternalExtractFunction(BotOwner _botOwner) : base(_botOwner)
        {
            _botOwner.Exfiltration._timeToExfiltration = float.MaxValue;
        }

        public override bool IsTryingToExtract() => BotOwner.Exfiltration.WannaLeave();

        public override bool TryInstructBotToExtract()
        {
            tryExtractSingleBot(BotOwner);
            LoggingController.LogDebug("Instructing " + BotOwner.GetText() + " to extract now");

            foreach (BotOwner follower in HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner))
            {
                if ((follower == null) || follower.IsDead)
                {
                    continue;
                }

                tryExtractSingleBot(follower);
                LoggingController.LogDebug("Instructing follower " + follower.GetText() + " to extract now");
            }

            return true;
        }

        private bool tryExtractSingleBot(BotOwner botOwner)
        {
            // Game time > _timeToExfiltration ? exfil now
            botOwner.Exfiltration._timeToExfiltration = 0f;

            return true;
        }
    }
}
