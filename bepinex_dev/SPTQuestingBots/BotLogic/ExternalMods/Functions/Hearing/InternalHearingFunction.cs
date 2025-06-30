using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing
{
    public class InternalHearingFunction : AbstractHearingFunction
    {
        public InternalHearingFunction(BotOwner _botOwner) : base(_botOwner)
        {

        }

        public override bool TryIgnoreHearing(bool value, bool ignoreUnderFire, float duration = 0)
        {
            return false;
        }
    }
}
