using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Extract
{
    public abstract class AbstractExtractFunction : AbstractBaseExternalFunctionWithMonitor
    {
        public AbstractExtractFunction(BotOwner _botOwner) : base(_botOwner)
        {
        }

        public abstract bool IsTryingToExtract();
        public abstract bool TryInstructBotToExtract();
    }
}
