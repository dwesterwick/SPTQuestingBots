using EFT;
using QuestingBots.ExternalMods.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.ExternalMods.Functions.Extract
{
    public abstract class AbstractExtractFunction : AbstractBaseExternalFunctionForBotWithMonitor
    {
        public AbstractExtractFunction(BotOwner _botOwner) : base(_botOwner)
        {
        }

        public abstract bool IsTryingToExtract();
        public abstract bool TryInstructBotToExtract();
    }
}
