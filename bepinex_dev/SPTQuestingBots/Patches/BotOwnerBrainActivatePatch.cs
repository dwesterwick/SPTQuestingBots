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
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("method_10", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            registerBot(__instance);
        }

        private static void registerBot(BotOwner __instance)
        {
            string roleName = __instance.Profile.Info.Settings.Role.ToString();

            LoggingController.LogInfo("Initial spawn type for bot " + __instance.GetText() + ": " + roleName);
            if (Helpers.BotBrainHelpers.WillBotBeAPMC(__instance))
            {
                Controllers.BotRegistrationManager.RegisterPMC(__instance);
            }
            else if (Helpers.BotBrainHelpers.WillBotBeABoss(__instance))
            {
                Controllers.BotRegistrationManager.RegisterBoss(__instance);
            }

            Controllers.BotRegistrationManager.WriteMessageForNewBotSpawn(__instance);

            BotLogic.HiveMind.BotHiveMindMonitor.RegisterBot(__instance);
            Singleton<GameWorld>.Instance.GetComponent<Components.DebugData>().RegisterBot(__instance);

            if (shouldMakeBotGroupHostileTowardAllBosses(__instance))
            {
                Controllers.BotRegistrationManager.MakeBotGroupHostileTowardAllBosses(__instance);
            }

            if (ConfigController.Config.BotSpawns.AdvancedEFTBotCountManagement && BotGenerator.GetAllGeneratedBotProfileIDs().Contains(__instance.Profile.Id))
            {
                LoggingController.LogInfo("Adjusting EFT bot counts for " + __instance.GetText() + "...");
                reduceBotCounts(__instance);
            }
        }

        private static bool shouldMakeBotGroupHostileTowardAllBosses(BotOwner bot)
        {
            BotType botType = Controllers.BotRegistrationManager.GetBotType(bot);

            float chance = 0;
            if (botType == BotType.Scav)
            {
                chance = ConfigController.Config.ChanceOfBeingHostileTowardBosses.Scav;
            }
            else if (botType == BotType.PScav)
            {
                chance = ConfigController.Config.ChanceOfBeingHostileTowardBosses.PScav;
            }
            else if (botType == BotType.PMC)
            {
                chance = ConfigController.Config.ChanceOfBeingHostileTowardBosses.PMC;
            }
            else if (botType == BotType.Boss)
            {
                chance = ConfigController.Config.ChanceOfBeingHostileTowardBosses.Boss;
            }

            System.Random random = new System.Random();
            if (random.Next(1, 100) <= chance)
            {
                return true;
            }

            return false;
        }

        private static void reduceBotCounts(BotOwner bot)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            FieldInfo allBotsCountField = AccessTools.Field(typeof(BotSpawner), "_allBotsCount");
            FieldInfo followersCountField = AccessTools.Field(typeof(BotSpawner), "_followersBotsCount");
            FieldInfo bossesCountField = AccessTools.Field(typeof(BotSpawner), "_bossBotsCount");

            if (bot.Profile.Info.Settings.IsFollower())
            {
                int followersCount = (int)followersCountField.GetValue(botSpawnerClass);
                followersCountField.SetValue(botSpawnerClass, followersCount - 1);
            }
            else if (bot.Profile.Info.Settings.IsBoss())
            {
                int bossesCount = (int)bossesCountField.GetValue(botSpawnerClass);
                bossesCountField.SetValue(botSpawnerClass, bossesCount - 1);
            }

            int allBotsCount = (int)allBotsCountField.GetValue(botSpawnerClass);
            allBotsCountField.SetValue(botSpawnerClass, allBotsCount - 1);
        }
    }
}
