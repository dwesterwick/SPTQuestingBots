﻿using System;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class InitBossSpawnLocationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass580).GetMethod("smethod_0", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BossLocationSpawn[] bossWaves, Action<BossLocationSpawn> spawnBossAction)
        {
            foreach (BossLocationSpawn bossWave in bossWaves)
            {
                // We only care about boss waves that will spawn immediately
                if ((bossWave.Time > 1) || !bossWave.ShallSpawn)
                {
                    continue;
                }

                // Ignore boss waves that require some type of interaction (i.e. Raiders that only spawn when a lever is pulled)
                if (bossWave.TriggerType != SpawnTriggerType.none)
                {
                    LoggingController.LogInfo("Ignoring " + bossWave.BossName + " boss wave. Trigger type: " + bossWave.TriggerType.ToString());
                    continue;
                }

                LoggingController.LogInfo("Spawn time for boss wave for " + bossWave.BossName + " is " + bossWave.Time);

                // EFT enables Cultist boss waves during daytime raids even though they'll never spawn. For now, ignore them.
                // TO DO: Check if they'll actually spawn based on time of day (which EFT SHOULD be doing anyway...)
                if ((bossWave.BossType == WildSpawnType.sectantPriest) || (bossWave.BossType == WildSpawnType.sectantWarrior))
                {
                    LoggingController.LogWarning("sectantPriest boss waves with initial PMC spawning may cause some bosses to spawn late.");
                    continue;
                }

                // EFT double-counts escorts and supports, so the total number of support bots needs to be subtracted from the total escort count
                int totalBots = 1 + bossWave.EscortCount;
                if (bossWave.Supports != null)
                {
                    totalBots -= bossWave.Supports.Sum(s => s.BossEscortAmount);
                }

                Controllers.BotRegistrationManager.ZeroWaveCount++;
                Controllers.BotRegistrationManager.ZeroWaveTotalBotCount += totalBots;
                Controllers.BotRegistrationManager.ZeroWaveTotalRogueCount += bossWave.BossName.ToLower() == "exusec" ? totalBots : 0;
            }

            LoggingController.LogInfo("Total inital bosses and followers " + Controllers.BotRegistrationManager.ZeroWaveTotalBotCount);
        }
    }
}
