using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot
{
    public abstract class AbstractLootFunction : AbstractBaseExternalFunctionWithMonitor
    {
        public AbstractLootFunction(BotOwner _botOwner) : base(_botOwner)
        {
        }

        public abstract bool IsSearchingForLoot();
        public abstract bool IsLooting();
        public abstract bool TryPreventBotFromLooting(float duration);
        public abstract bool TryForceBotToScanLoot();
    }
}
