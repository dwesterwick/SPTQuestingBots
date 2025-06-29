using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing
{
    public class SAINHearingFunction : AbstractHearingFunction
    {
        public SAINHearingFunction(BotOwner _botOwner) : base(_botOwner)
        {

        }

        public override bool TryIgnoreHearing(bool value, bool ignoreUnderFire, float duration = 0)
        {
            if (!SAIN.Plugin.SAINInterop.IgnoreHearing(BotOwner, value, false, duration))
            {
                LoggingController.LogWarning("Cannot instruct " + BotOwner.GetText() + " to ignore hearing. SAIN Interop not initialized properly or is outdated.");

                return false;
            }

            LoggingController.LogDebug("Instructing " + BotOwner.GetText() + " to " + (value ? "" : "not ") + "ignore hearing for " + duration + "s");

            return true;
        }
    }
}
