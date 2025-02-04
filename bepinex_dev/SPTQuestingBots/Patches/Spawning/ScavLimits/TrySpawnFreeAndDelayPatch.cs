using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning.ScavLimits
{
    public class TrySpawnFreeAndDelayPatch : ModulePatch
    {
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
            if (NonWavesSpawnScenarioCreatePatch.SpawnedScavs + pendingScavCount <= QuestingBotsPluginConfig.ScavSpawnLimitThreshold.Value)
            {
                return allowSpawn(pendingScavCount);
            }

            // The bot cap for Factory is sometimes set at 0, in which case the Scav spawn rate limit cannot be used
            if (locationData.MaxTotalBots == 0)
            {
                allowSpawn(pendingScavCount);
            }

            float raidTimeElapsedFraction = 1f - Helpers.RaidHelpers.GetRaidTimeRemainingFraction();
            float totalScavsAllowedToSpawn = locationData.MaxTotalBots * QuestingBotsPluginConfig.TotalScavSpawnLimitFraction.Value;
            int totalScavsAllowedToSpawnNow = (int)Math.Round(totalScavsAllowedToSpawn * raidTimeElapsedFraction, 0);

            // Prevent too many total Scavs from spawning at this point in the raid
            if (NonWavesSpawnScenarioCreatePatch.SpawnedScavs + pendingScavCount > totalScavsAllowedToSpawnNow)
            {
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
            Controllers.LoggingController.LogWarning("Prevented " + scavCount + " Scav(s) from spawning due to: " + reason.ToString());
            return false;
        }
    }
}
