using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace QuestingBots.BotLogic.ExternalMods.Functions
{
    public class AbstractBaseExternalFunctionForBot : IAbstractBaseExternalFunction
    {
        protected BotOwner BotOwner { get; private set; }

        public AbstractBaseExternalFunctionForBot(BotOwner botOwner)
        {
            BotOwner = botOwner;
        }
    }
}
