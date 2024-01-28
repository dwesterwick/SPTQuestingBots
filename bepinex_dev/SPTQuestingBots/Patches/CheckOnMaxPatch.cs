using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class CheckOnMaxPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod("CheckOnMax", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(int wantSpawn, ref int toDelay, ref int toSpawn, bool calcOnlySimpleBots)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            FieldInfo maxBotsField = AccessTools.Field(typeof(BotSpawner), "_maxBots");
            int maxBots = (int)maxBotsField.GetValue(botSpawnerClass);

            LoggingController.LogInfo("Max Bots: " + maxBots + ", All Bots: " + botSpawnerClass.AliveAndLoadingBotsCount + ", To Delay: " + toDelay + ", To Spawn: " + toSpawn);
        }
    }
}
