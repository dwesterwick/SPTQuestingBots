using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches.Debug
{
    public class IsPlayerEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotDifficultySettingsClass).GetMethod(nameof(BotDifficultySettingsClass.IsPlayerEnemy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotDifficultySettingsClass __instance, bool __result, IPlayer player)
        {
            if ((__instance.wildSpawnType_0 != WildSpawnType.pmcBEAR) && (__instance.wildSpawnType_0 != WildSpawnType.pmcUSEC))
            {
                return;
            }

            if ((player.Profile.Info.Settings.Role != WildSpawnType.pmcBEAR) && (player.Profile.Info.Settings.Role != WildSpawnType.pmcUSEC))
            {
                return;
            }

            string message = "[BotDifficultySettingsClass.IsPlayerEnemy]" + player.GetText() + "(" + player.Profile.Info.Settings.Role + ") is enemy of " + __instance.wildSpawnType_0 + " group: " + __result;

            AdditionalHostilitySettings additionalHostilitySettings = getAdditionalHostilitySettings(__instance);
            string enemyChanceMessage = "[BotDifficultySettingsClass.IsPlayerEnemy] BearEnemyChance=" + additionalHostilitySettings.BearEnemyChance + ", UsecEnemyChance=" + additionalHostilitySettings.UsecEnemyChance;// + ", ChancedEnemies={" + string.Join(", ", additionalHostilitySettings.ChancedEnemies.Select(x => x.Role + "=" + x.EnemyChance)) + "}";

            if (__result)
            {
                //LoggingController.LogInfo(enemyChanceMessage);
            }
            else
            {
                LoggingController.LogWarning(enemyChanceMessage);
            }
        }

        private static AdditionalHostilitySettings getAdditionalHostilitySettings(BotDifficultySettingsClass __instance)
        {
            AdditionalHostilitySettings[] additionalHostilitySettings = Singleton<IBotGame>.Instance.BotsController.BotLocationModifier.AdditionalHostilitySettings;
            
            foreach (AdditionalHostilitySettings additionalHostilitySettings3 in additionalHostilitySettings)
            {
                if (additionalHostilitySettings3.BotRole == __instance.wildSpawnType_0)
                {
                    return additionalHostilitySettings3;
                }
            }

            throw new InvalidOperationException("Cannot find AdditionalHostilitySettings for " + __instance.wildSpawnType_0);
        }
    }
}
