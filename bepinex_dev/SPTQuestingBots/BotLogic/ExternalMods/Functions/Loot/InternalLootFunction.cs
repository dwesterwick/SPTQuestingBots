using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot
{
    public class InternalLootFunction : AbstractLootFunction
    {
        public override string MonitoredLayerName => "Looting";

        public InternalLootFunction(BotOwner _botOwner) : base(_botOwner)
        {
            
        }

        public override bool IsSearchingForLoot()
        {
            return false;
        }

        public override bool IsLooting()
        {
            return false;
        }

        public override bool TryPreventBotFromLooting(float duration)
        {
            return false;
        }

        public override bool TryForceBotToScanLoot()
        {
            return false;
        }
    }
}
