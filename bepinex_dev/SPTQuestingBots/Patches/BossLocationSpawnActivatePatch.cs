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
            int maxRogues = 5;
            int botCount = 1 + bossWave.EscortCount;
            if (bossWave.BossName.ToLower() == "exusec")
            {
                if (BotGenerator.SpawnedRogueCount + botCount > maxRogues)
                {
                    LoggingController.LogWarning("Suppressing boss wave or too many Rogues will be on the map");
                    return false;
                }

                LoggingController.LogInfo("Spawning " + (BotGenerator.SpawnedRogueCount + botCount) + "/" + maxRogues + " Rogues...");
            }

            BotGenerator.SpawnedBossWaves++;
            BotGenerator.SpawnedRogueCount += botCount;
            bossWave.IgnoreMaxBots = true;

            string message = "Spawning boss wave ";
            message += BotGenerator.SpawnedBossWaves + "/" + BotGenerator.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " with " + botCount + " total bots";
            message += "...";

            LoggingController.LogInfo(message);
            return true;
        }
    }
}
