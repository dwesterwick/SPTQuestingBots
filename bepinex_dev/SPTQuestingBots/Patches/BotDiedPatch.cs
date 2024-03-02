using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;

namespace SPTQuestingBots.Patches
{
    public class BotDiedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod("BotDied", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotOwner bot)
        {
            //LoggingController.LogInfo("Bot " + bot.GetText() + " died. Updating BotSpawner data...");

            bot.IsDead = true;

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            FieldInfo botsClassField = AccessTools.Field(typeof(BotSpawner), "_bots");
            BotsClass botsClass = (BotsClass)botsClassField.GetValue(botSpawnerClass);
            botsClass.Remove(bot);

            FieldInfo onBotRemovedField = AccessTools.Field(typeof(BotSpawner), "OnBotRemoved");
            Action<BotOwner> onBotRemoved = (Action<BotOwner>)onBotRemovedField.GetValue(botSpawnerClass);
            if (onBotRemoved != null)
            {
                onBotRemoved(bot);
            }

            return false;
        }
    }
}
