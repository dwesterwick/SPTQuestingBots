using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Hearing
{
    public abstract class AbstractHearingFunction : AbstractBaseExternalFunction
    {
        public AbstractHearingFunction(BotOwner _botOwner) : base(_botOwner)
        {
            
        }

        public abstract bool TryIgnoreHearing(bool value, bool ignoreUnderFire, float duration = 0);
    }
}
