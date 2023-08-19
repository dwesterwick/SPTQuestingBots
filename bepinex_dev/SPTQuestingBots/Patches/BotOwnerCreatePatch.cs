using Aki.Reflection.Patching;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class BotOwnerCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __result)
        {
            LoggingController.LogInfo("Spawned bot " + __result.Profile.Nickname + " (Brain: " + __result.Brain.GetType().FullName + ")");

            BotGenerator.SpawnedBotCount++;
        }
    }
}
