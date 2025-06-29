using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot
{
    public class LootingBotsLootFunction : AbstractLootFunction
    {
        public override string MonitoredLayerName => "Looting";

        public LootingBotsLootFunction(BotOwner _botOwner) : base(_botOwner)
        {
            
        }

        public override bool TryPreventBotFromLooting(float duration)
        {
            if (LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, duration))
            {
                LoggingController.LogDebug("Preventing " + BotOwner.GetText() + " from looting");

                return true;
            }
            else
            {
                LoggingController.LogWarning("Cannot prevent " + BotOwner.GetText() + " from looting. Looting Bots Interop not initialized properly or is outdated.");
            }

            return false;
        }

        public override bool TryForceBotToScanLoot()
        {
            return false;
        }
    }
}
