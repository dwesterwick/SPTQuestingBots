using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace QuestingBots.BotLogic.ExternalMods.Functions
{
    public class AbstractBaseExternalFunction
    {
        protected BotOwner BotOwner { get; private set; }

        public AbstractBaseExternalFunction(BotOwner botOwner)
        {
            BotOwner = botOwner;
        }
    }
}
