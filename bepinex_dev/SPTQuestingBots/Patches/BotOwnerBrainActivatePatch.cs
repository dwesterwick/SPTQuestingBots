using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerBrainActivatePatch : ModulePatch
    {
        private static FieldInfo allBotsCountField;
        private static FieldInfo followersCountField;
        private static FieldInfo bossesCountField;

        protected override MethodBase GetTargetMethod()
        {
            allBotsCountField = AccessTools.Field(typeof(BotSpawner), "_allBotsCount");
            followersCountField = AccessTools.Field(typeof(BotSpawner), "_followersBotsCount");
            bossesCountField = AccessTools.Field(typeof(BotSpawner), "_bossBotsCount");

            return typeof(BotOwner).GetMethod("method_10", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotOwner __instance)
        {
            registerBot(__instance);

            if (ConfigController.Config.BotSpawns.AdvancedEFTBotCountManagement.Enabled && BotGenerator.GetAllGeneratedBotProfileIDs().Contains(__instance.Profile.Id))
            {
                reduceBotCounts(__instance);
            }

            if (shouldMakeBotGroupHostileTowardAllBosses(__instance))
            {
                Controllers.BotRegistrationManager.MakeBotGroupHostileTowardAllBosses(__instance);
            }

            // Fix for bots getting stuck in Standby when enemy PMC's are near them
            __instance.StandBy.CanDoStandBy = false;
        }

        private static void registerBot(BotOwner __instance)
        {
            string roleName = __instance.Profile.Info.Settings.Role.ToString();

            LoggingController.LogInfo("Initial spawn type for bot " + __instance.GetText() + ": " + roleName);
            if (__instance.WillBeAPMC())
            {
                Controllers.BotRegistrationManager.RegisterPMC(__instance);
            }
            else if (__instance.WillBeABoss())
            {
                Controllers.BotRegistrationManager.RegisterBoss(__instance);
            }

            Controllers.BotRegistrationManager.WriteMessageForNewBotSpawn(__instance);

            BotLogic.HiveMind.BotHiveMindMonitor.RegisterBot(__instance);
            Singleton<GameWorld>.Instance.GetComponent<Components.DebugData>().RegisterBot(__instance);

            if (__instance.IsARegisteredPMC() || __instance.WillBeAPlayerScav())
            {
                registerBotAsHumanPlayer(__instance);
            }
        }

        private static void registerBotAsHumanPlayer(BotOwner __instance)
        {
            if (!ConfigController.Config.BotSpawns.Enabled)
            {
                return;
            }

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            botSpawnerClass.AddPlayer(__instance.GetPlayer());
            __instance.GetPlayer().OnPlayerDead += deletePlayer;

            Controllers.LoggingController.LogWarning("Registered " + __instance.GetText() + " as a human player in EFT");
        }

        private static void deletePlayer(Player player, IPlayer lastAgressor, DamageInfoStruct damage, EBodyPart part)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            botSpawnerClass.DeletePlayer(player.GetPlayer());
        }

        private static bool shouldMakeBotGroupHostileTowardAllBosses(BotOwner bot)
        {
            BotType botType = Controllers.BotRegistrationManager.GetBotType(bot);

            float chance = ConfigController.Config.ChanceOfBeingHostileTowardBosses.GetValue(botType) ?? 0;

            System.Random random = new System.Random();
            if (random.Next(1, 100) <= chance)
            {
                return true;
            }

            return false;
        }

        private static void reduceBotCounts(BotOwner bot)
        {
            LoggingController.LogDebug("Adjusting EFT bot counts for " + bot.GetText() + "...");

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

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
