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
        private const float TIME_WINDOW_TO_CHECK = 60f * 5f;

        private static FieldInfo nextRetryTimeField = null;

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
            nextRetryTimeField = AccessTools.Field(typeof(NonWavesSpawnScenario), "float_2");

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
            int pendingScavCount = data.Profiles.Count(p => isScavProfile(p));
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
            int totalAliveScavs = Singleton<GameWorld>.Instance.AllAlivePlayersList.Count(p => isScavProfile(p.Profile));
            if (totalAliveScavs + pendingScavCount > QuestingBotsPluginConfig.ScavMaxAliveLimit.Value)
            {
                return blockSpawn(pendingScavCount, ScavSpawnBlockReason.MaxAliveScavs);
            }

            // Check if the rate limit should be used
            if (NonWavesSpawnScenarioCreatePatch.TotalSpawnedScavs + pendingScavCount <= QuestingBotsPluginConfig.ScavSpawnLimitThreshold.Value)
            {
                return allowSpawn(pendingScavCount);
            }

            // The bot cap for Factory is sometimes set at 0, in which case the Scav spawn rate limit cannot be used
            if (locationData.MaxTotalBots == 0)
            {
                allowSpawn(pendingScavCount);
            }

            int recentlySpawnedScavs = NonWavesSpawnScenarioCreatePatch.GetSpawnedScavCount(TIME_WINDOW_TO_CHECK);
            float recentScavSpawnRate = recentlySpawnedScavs * 60f / TIME_WINDOW_TO_CHECK;

            // Prevent too many Scavs from spawning in a short period of time
            if (recentScavSpawnRate > QuestingBotsPluginConfig.TotalScavSpawnLimit.Value)
            {
                LoggingController.LogWarning(recentlySpawnedScavs + " Scavs have spawned in the last " + TIME_WINDOW_TO_CHECK + "s. Rate=" + recentScavSpawnRate);
                return blockSpawn(pendingScavCount, ScavSpawnBlockReason.ScavRateLimit);
            }

            return allowSpawn(pendingScavCount);
        }

        private static bool isScavProfile(Profile profile)
        {
            return !profile.WillBeAPlayerScav() && scavRoles.Contains(profile.Info.Settings.Role);
        }

        private static bool allowSpawn(int scavCount)
        {
            NonWavesSpawnScenarioCreatePatch.AddSpawnedScavs(scavCount);
            return true;
        }

        private static bool blockSpawn(int scavCount, ScavSpawnBlockReason reason)
        {
            nextRetryTimeField.SetValue(NonWavesSpawnScenarioCreatePatch.MostRecentNonWavesSpawnScenario, ConfigController.Config.BotSpawns.NonWaveRetryDelayAfterBlocked);

            Controllers.LoggingController.LogWarning("Prevented " + scavCount + " Scav(s) from spawning due to: " + reason.ToString());
            return false;
        }
    }
}
