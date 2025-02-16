using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning.ScavLimits
{
    public class TrySpawnFreeAndDelayPatch : ModulePatch
    {
        private static FieldInfo nextRetryTimeDelayField = null;

        private enum ScavSpawnBlockReason
        {
            MaxAliveScavs,
            ScavRateLimit,
        }

        private static readonly WildSpawnType[] scavRoles = new WildSpawnType[]
        {
            WildSpawnType.assault,
            WildSpawnType.assaultGroup,
            WildSpawnType.cursedAssault,
            WildSpawnType.marksman,
        };

        protected override MethodBase GetTargetMethod()
        {
            nextRetryTimeDelayField = AccessTools.Field(typeof(NonWavesSpawnScenario), "float_2");

            return typeof(BotSpawner).GetMethod(nameof(BotSpawner.TrySpawnFreeAndDelay), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotCreationDataClass data)
        {
            if (!QuestingBotsPluginConfig.ScavLimitsEnabled.Value)
            {
                return true;
            }

            if (NonWavesSpawnScenarioCreatePatch.MostRecentNonWavesSpawnScenario == null)
            {
                return true;
            }

            // Check how many Scavs are queued to spawn, and allow all other roles to spawn
            int pendingScavCount = data.Profiles.Count(p => isNormalScavProfile(p));
            if (pendingScavCount == 0)
            {
                return allowSpawn(pendingScavCount);
            }

            // Only limit Scav spawns when the new spawning system is being used
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            if (!locationData.CurrentLocation.NewSpawn)
            {
                return allowSpawn(pendingScavCount);
            }

            // Ensure the maximum alive count for Scavs will not be exceeded
            int totalAliveScavs = Singleton<GameWorld>.Instance.AllAlivePlayersList.Count(p => isNormalScavProfile(p.Profile));
            if (totalAliveScavs + pendingScavCount > QuestingBotsPluginConfig.ScavMaxAliveLimit.Value)
            {
                return blockSpawn(pendingScavCount, ScavSpawnBlockReason.MaxAliveScavs);
            }

            // Check if the rate limit should be used
            if (NonWavesSpawnScenarioCreatePatch.TotalSpawnedScavs + pendingScavCount <= QuestingBotsPluginConfig.ScavSpawnLimitThreshold.Value)
            {
                return allowSpawn(pendingScavCount);
            }

            // The bot cap for Factory is sometimes set at 0, in which case the Scav spawn rate limit cannot be used (assuming the new spawning system is enabled)
            if (locationData.MaxTotalBots == 0)
            {
                allowSpawn(pendingScavCount);
            }

            float timeWindow = ConfigController.Config.BotSpawns.EftNewSpawnSystemAdjustments.ScavSpawnRateTimeWindow;
            int recentlySpawnedScavs = NonWavesSpawnScenarioCreatePatch.GetSpawnedScavCount(timeWindow, true);
            float recentScavSpawnRate = recentlySpawnedScavs * 60f / timeWindow;

            // Prevent too many Scavs from spawning in a short period of time
            if (recentScavSpawnRate >= QuestingBotsPluginConfig.ScavSpawnRateLimit.Value)
            {
                return blockSpawn(pendingScavCount, ScavSpawnBlockReason.ScavRateLimit);
            }

            return allowSpawn(pendingScavCount);
        }

        private static bool isNormalScavProfile(Profile profile)
        {
            return !profile.WillBeAPlayerScav() && scavRoles.Contains(profile.Info.Settings.Role);
        }

        private static bool allowSpawn(int scavCount)
        {
            NonWavesSpawnScenarioCreatePatch.AddSpawnedScavs(scavCount);

            if (scavCount > 0)
            {
                logScavSpawnRate();
            }

            return true;
        }

        private static bool blockSpawn(int scavCount, ScavSpawnBlockReason reason)
        {
            Controllers.LoggingController.LogDebug("Prevented " + scavCount + " Scav(s) from spawning due to: " + reason.ToString());
            logScavSpawnRate();

            float retryDelay = ConfigController.Config.BotSpawns.EftNewSpawnSystemAdjustments.NonWaveRetryDelayAfterBlocked;
            nextRetryTimeDelayField.SetValue(NonWavesSpawnScenarioCreatePatch.MostRecentNonWavesSpawnScenario, retryDelay);
            
            return false;
        }

        private static void logScavSpawnRate()
        {
            float timeWindow = ConfigController.Config.BotSpawns.EftNewSpawnSystemAdjustments.ScavSpawnRateTimeWindow;
            int recentlySpawnedScavs = NonWavesSpawnScenarioCreatePatch.GetSpawnedScavCount(timeWindow, true);
            float recentScavSpawnRate = recentlySpawnedScavs * 60f / timeWindow;

            Controllers.LoggingController.LogDebug(recentlySpawnedScavs + " Scavs have spawned in the last " + timeWindow + "s. Rate=" + recentScavSpawnRate);
        }
    }
}
