using Aki.Reflection.Patching;
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
    public class InitBossSpawnLocationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass616).GetMethod("smethod_0", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BossLocationSpawn[] bossWaves, Action<BossLocationSpawn> spawnBossAction)
        {
            foreach (BossLocationSpawn bossWave in bossWaves)
            {
                if ((bossWave.Time <= 1) && bossWave.ShallSpawn)
                {
                    int totalBots = 1 + bossWave.EscortCount;

                    LoggingController.LogInfo("Spawn time for boss wave for " + bossWave.BossName + " is " + bossWave.Time);
                    LocationController.ZeroWaveCount++;
                    LocationController.ZeroWaveTotalBotCount += totalBots;
                    LocationController.ZeroWaveTotalRogueCount += bossWave.BossName.ToLower() == "exusec" ? totalBots : 0;
                }
            }

            LoggingController.LogInfo("Total inital bosses and followers " + LocationController.ZeroWaveTotalBotCount);
        }
    }
}
