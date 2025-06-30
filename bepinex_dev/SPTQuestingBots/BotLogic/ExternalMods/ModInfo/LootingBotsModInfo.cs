using EFT;
using SPTQuestingBots.BotLogic.ExternalMods.Functions.Loot;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.ExternalMods.ModInfo
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
                LoggingController.LogWarning("Looting Bots Interop not detected. Cannot instruct bots to loot.");
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
