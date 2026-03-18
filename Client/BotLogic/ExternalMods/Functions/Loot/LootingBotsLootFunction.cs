using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;

namespace QuestingBots.BotLogic.ExternalMods.Functions.Loot
{
    public class LootingBotsLootFunction : AbstractLootFunction
    {
        public override string MonitoredLayerName => "Looting";

        private static string _lootingLogicName => "Looting";

        public LootingBotsLootFunction(BotOwner _botOwner) : base(_botOwner)
        {
            
        }

        public override bool IsSearchingForLoot() => IsMonitoredLayerActive();

        public override bool IsLooting()
        {
            string activeLogicName = BotOwner.GetActiveLogicTypeName() ?? "null";
            return activeLogicName.Contains(_lootingLogicName);
        }

        public override bool TryPreventBotFromLooting(float duration)
        {
            if (LootingBots.LootingBotsInterop.TryPreventBotFromLooting(BotOwner, duration))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Preventing " + BotOwner.GetText() + " from looting");

                return true;
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot prevent " + BotOwner.GetText() + " from looting. Looting Bots Interop not initialized properly or is outdated.");
            }

            return false;
        }

        public override bool TryForceBotToScanLoot()
        {
            if (LootingBots.LootingBotsInterop.TryForceBotToScanLoot(BotOwner))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Instructing " + BotOwner.GetText() + " to loot now");

                return true;
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot instruct " + BotOwner.GetText() + " to loot. Looting Bots Interop not initialized properly or is outdated.");
            }

            return false;
        }
    }
}
