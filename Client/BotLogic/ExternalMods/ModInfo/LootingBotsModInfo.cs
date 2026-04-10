using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.ExternalMods.Functions.Loot;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class LootingBotsModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "me.skwizzy.lootingbots";

        public override bool CheckInteropAvailability()
        {
            if (LootingBots.LootingBotsInterop.Init())
            {
                CanUseInterop = true;
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Looting Bots Interop not detected. Cannot instruct bots to loot.");
            }

            return CanUseInterop;
        }

        public override AbstractLootFunction CreateLootFunction(BotOwner _botOwner)
        {
            if (!CanUseInterop)
            {
                return base.CreateLootFunction(_botOwner);
            }

            return new LootingBotsLootFunction(_botOwner);
        }
    }
}
