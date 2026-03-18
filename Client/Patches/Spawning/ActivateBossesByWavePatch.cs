using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Utils;

namespace QuestingBots.Patches.Spawning
{
    public class ActivateBossesByWavePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(
                nameof(BotsController.ActivateBotsByWave),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(BossLocationSpawn) },
                null);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(ref BossLocationSpawn wave)
        {
            if (!GameStartPatch.IsDelayingGameStart)
            {
                if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Allowing spawn of boss wave " + wave.BossName + "...");
                }

                return !shouldSuppressBossWave(ref wave);
            }

            GameStartPatch.AddMissedBossWave(wave);

            if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Delaying spawn of boss wave " + wave.BossName + "...");
            }

            return false;
        }

        private static bool shouldSuppressBossWave(ref BossLocationSpawn bossWave)
        {
            if (!ConfigController.Config.BotSpawns.LimitInitialBossSpawns.Enabled)
            {
                return false;
            }

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

                    Singleton<LoggingUtil>.Instance.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many Rogues will be on the map");
                    return true;
                }
            }

            // Prevent too many bosses from spawning, or they will prevent other bots from spawning
            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if ((pmcGenerator != null) && (pmcGenerator.SpawnedGroupCount == 0) && (BotRegistrationManager.SpawnedBossCount + botCount > ConfigController.Config.BotSpawns.LimitInitialBossSpawns.MaxInitialBosses))
            {
                BotRegistrationManager.ZeroWaveTotalBotCount -= botCount;

                Singleton<LoggingUtil>.Instance.LogWarning("Suppressing boss wave (" + botCount + " bots) or too many bosses will be on the map");
                return true;
            }

            BotRegistrationManager.SpawnedBossWaves++;
            BotRegistrationManager.SpawnedBossCount += botCount;
            if (bossWave.BossName.ToLower() == "exusec")
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Spawning " + (BotRegistrationManager.SpawnedRogueCount + botCount) + "/" + ConfigController.Config.BotSpawns.LimitInitialBossSpawns.MaxInitialRogues + " Rogues...");
                BotRegistrationManager.SpawnedRogueCount += botCount;
            }

            string message = "Spawning boss wave ";
            message += BotRegistrationManager.SpawnedBossWaves + "/" + BotRegistrationManager.ZeroWaveCount;
            message += " for bot type " + bossWave.BossName;
            message += " with " + botCount + " total bots";
            message += "...";
            Singleton<LoggingUtil>.Instance.LogInfo(message);

            return false;
        }
    }
}
