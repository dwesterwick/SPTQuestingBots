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

            if ((BotGenerator.SpawnedInitialPMCCount == 0) && (LocationController.SpawnedBotCount + botCount > LocationController.MaxInitialBosses))
            {
                LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many bosses will be on the map");
                return false;
            }

            if (bossWave.BossName.ToLower() == "exusec")
            {
                if (LocationController.SpawnedRogueCount + botCount > LocationController.MaxInitialRogues)
                {
                    LocationController.ZeroWaveTotalBotCount -= botCount;
                    LocationController.ZeroWaveTotalRogueCount -= botCount;

                    LoggingController.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many Rogues will be on the map");
                    return false;
                }

                LoggingController.LogInfo("Spawning " + (LocationController.SpawnedRogueCount + botCount) + "/" + LocationController.MaxInitialRogues + " Rogues...");
            }

            string message = "Spawning boss wave ";
            message += LocationController.SpawnedBossWaves + "/" + LocationController.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " with " + botCount + " total bots";
            message += "...";
            LoggingController.LogInfo(message);

            LocationController.SpawnedBossWaves++;
            if (bossWave.BossName.ToLower() == "exusec")
            {
                LocationController.SpawnedRogueCount += botCount;
            }

            // This doesn't seem to work
            //bossWave.IgnoreMaxBots = true;
            
            return true;
        }
    }
}
