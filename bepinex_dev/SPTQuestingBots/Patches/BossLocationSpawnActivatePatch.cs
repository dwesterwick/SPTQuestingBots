using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class BossLocationSpawnActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            Type targetType = localGameType.GetNestedType("Class1367", BindingFlags.Public | BindingFlags.Instance);

            return targetType.GetMethod("method_1", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref BossLocationSpawn bossWave)
        {
            // EFT double-counts escorts and supports, so the total number of support bots needs to be subtracted from the total escort count
            int botCount = 1 + bossWave.EscortCount;
            if (bossWave.Supports != null)
            {
                botCount -= bossWave.Supports.Sum(s => s.BossEscortAmount);
            }

            if (bossWave.BossName.ToLower() == "exusec")
            {
                // Prevent too many Rogues from spawning, or they will prevent other bots from spawning
                if (BotRegistrationManager.SpawnedRogueCount + botCount > ConfigController.Config.BotSpawns.LimitInitialBossSpawns.MaxInitialRogues)
                {
                    BotRegistrationManager.ZeroWaveTotalBotCount -= botCount;
                    BotRegistrationManager.ZeroWaveTotalRogueCount -= botCount;

                    LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many Rogues will be on the map");
                    return false;
                }
            }

            // Prevent too many bosses from spawning, or they will prevent other bots from spawning
            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if ((pmcGenerator != null) && (pmcGenerator.SpawnedGroupCount == 0) && (BotRegistrationManager.SpawnedBossCount + botCount > ConfigController.Config.BotSpawns.LimitInitialBossSpawns.MaxInitialBosses))
            {
                BotRegistrationManager.ZeroWaveTotalBotCount -= botCount;

                LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many bosses will be on the map");
                return false;
            }

            BotRegistrationManager.SpawnedBossWaves++;
            BotRegistrationManager.SpawnedBossCount += botCount;
            if (bossWave.BossName.ToLower() == "exusec")
            {
                LoggingController.LogInfo("Spawning " + (BotRegistrationManager.SpawnedRogueCount + botCount) + "/" + ConfigController.Config.BotSpawns.LimitInitialBossSpawns.MaxInitialRogues + " Rogues...");
                BotRegistrationManager.SpawnedRogueCount += botCount;
            }

            string message = "Spawning boss wave ";
            message += BotRegistrationManager.SpawnedBossWaves + "/" + BotRegistrationManager.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " with " + botCount + " total bots";
            message += "...";
            LoggingController.LogInfo(message);

            return true;
        }
    }
}
