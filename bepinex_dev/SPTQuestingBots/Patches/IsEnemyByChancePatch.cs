using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class IsEnemyByChancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotDifficultySettingsClass).GetMethod("IsEnemyByChance", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(bool __result, BotOwner bot, BotDifficulty ___botDifficulty_0, WildSpawnType ___wildSpawnType_0)
        {
            LoggingController.LogInfo("IsEnemyByChance -> " + bot.GetText() + " for " + ___botDifficulty_0 + " " + ___wildSpawnType_0 + ": " + __result);

            AdditionalHostilitySettings[] additionalHostilitySettings = Comfort.Common.Singleton<IBotGame>.Instance.BotsController.BotLocationModifier.AdditionalHostilitySettings;
            if (additionalHostilitySettings == null)
            {
                LoggingController.LogInfo("additionalHostilitySettings is null");
                return;
            }

            foreach (AdditionalHostilitySettings additionalHostilitySettings2 in additionalHostilitySettings)
            {
                if (additionalHostilitySettings2.BotRole != ___wildSpawnType_0)
                {
                    continue;
                }

                int j = 0;
                while (j < additionalHostilitySettings2.ChancedEnemies.Length)
                {
                    AdditionalHostilitySettings.ChancedEnemy chancedEnemy = additionalHostilitySettings2.ChancedEnemies[j];
                    if (chancedEnemy.Role == bot.Profile.Info.Settings.Role)
                    {
                        LoggingController.LogInfo("Chance of enemy for " + chancedEnemy.Role + ": " + chancedEnemy.EnemyChance);
                        return;
                    }

                    j++;
                }
            }
        }
    }
}
