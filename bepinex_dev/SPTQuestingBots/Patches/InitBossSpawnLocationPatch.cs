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
                if ((bossWave.Time > 1) || !bossWave.ShallSpawn)
                {
                    continue;
                }

                LoggingController.LogInfo("Spawn time for boss wave for " + bossWave.BossName + " is " + bossWave.Time);

                if ((bossWave.BossType == WildSpawnType.sectantPriest) || (bossWave.BossType == WildSpawnType.sectantWarrior))
                {
                    LoggingController.LogWarning("sectantPriest boss waves with initial PMC spawning may cause some bosses to spawn late.");
                    continue;
                }

                int totalBots = 1 + bossWave.EscortCount;
                if (bossWave.Supports != null)
                {
                    totalBots -= bossWave.Supports.Sum(s => s.BossEscortAmount);
                }

                LocationController.ZeroWaveCount++;
                LocationController.ZeroWaveTotalBotCount += totalBots;
                LocationController.ZeroWaveTotalRogueCount += bossWave.BossName.ToLower() == "exusec" ? totalBots : 0;
            }

            LoggingController.LogInfo("Total inital bosses and followers " + LocationController.ZeroWaveTotalBotCount);
        }
    }
}
