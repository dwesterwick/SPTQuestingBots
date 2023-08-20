using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class BossLocationSpawnActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            Type targetType = localGameType.GetNestedType("Class1265", BindingFlags.NonPublic | BindingFlags.Instance);

            return targetType.GetMethod("method_1", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref BossLocationSpawn bossWave)
        {
            int botCount = 1 + bossWave.EscortCount;
            if (bossWave.Supports != null)
            {
                botCount -= bossWave.Supports.Sum(s => s.BossEscortAmount);
            }

            if (bossWave.BossName.ToLower() == "exusec")
            {
                if (LocationController.SpawnedRogueCount + botCount > ConfigController.Config.InitialPMCSpawns.MaxInitialRogues)
                {
                    LocationController.ZeroWaveTotalBotCount -= botCount;
                    LocationController.ZeroWaveTotalRogueCount -= botCount;

                    LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many Rogues will be on the map");
                    return false;
                }

                LoggingController.LogInfo("Spawning " + (LocationController.SpawnedRogueCount + botCount) + "/" + ConfigController.Config.InitialPMCSpawns.MaxInitialRogues + " Rogues...");
            }

            if ((BotGenerator.SpawnedInitialPMCCount == 0) && (LocationController.SpawnedBossCount + botCount > ConfigController.Config.InitialPMCSpawns.MaxInitialBosses))
            {
                LocationController.ZeroWaveTotalBotCount -= botCount;

                LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many bosses will be on the map");
                return false;
            }

            LocationController.SpawnedBossWaves++;
            LocationController.SpawnedBossCount += botCount;
            if (bossWave.BossName.ToLower() == "exusec")
            {
                LocationController.SpawnedRogueCount += botCount;
            }

            string message = "Spawning boss wave ";
            message += LocationController.SpawnedBossWaves + "/" + LocationController.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " with " + botCount + " total bots";
            message += "...";
            LoggingController.LogInfo(message);

            // This doesn't seem to work
            //bossWave.IgnoreMaxBots = true;
            
            return true;
        }
    }
}
