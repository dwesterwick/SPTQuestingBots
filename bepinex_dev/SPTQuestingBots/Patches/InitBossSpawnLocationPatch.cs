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
        public static int ZeroWaveCount { get; set; } = 0;
        public static int ZeroWaveBotCount { get; set; } = 0;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass616).GetMethod("smethod_0", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BossLocationSpawn[] bossWaves, Action<BossLocationSpawn> spawnBossAction)
        {
            BossLocationSpawnActivatePatch.SpawnedWaves = 0;
            BotOwnerCreatePatch.SpawnedBotCount = 0;

            ZeroWaveCount = 0;
            ZeroWaveBotCount = 0;
            foreach (BossLocationSpawn bossWave in bossWaves)
            {
                if ((bossWave.Time <= 1) && bossWave.ShallSpawn)
                {
                    LoggingController.LogInfo("Spawn time for boss wave for " + bossWave.BossName + " is " + bossWave.Time);
                    ZeroWaveCount++;
                    ZeroWaveBotCount += 1;
                    ZeroWaveBotCount += bossWave.EscortCount;
                }
            }

            LoggingController.LogInfo("Total inital bosses and followers " + ZeroWaveBotCount);
        }
    }
}
