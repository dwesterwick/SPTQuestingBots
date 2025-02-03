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

namespace SPTQuestingBots.Patches.Spawning
{
    public class TrySpawnFreeAndDelayPatch : ModulePatch
    {
        private static readonly WildSpawnType[] scavRoles = new WildSpawnType[] { WildSpawnType.assault, WildSpawnType.assaultGroup, WildSpawnType.marksman };

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod(nameof(BotSpawner.TrySpawnFreeAndDelay), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotCreationDataClass data)
        {
            if (NonWavesSpawnScenarioCreatePatch.MostRecentNonWavesSpawnScenario == null)
            {
                return true;
            }

            int scavCount = data.Profiles.Sum(p => !p.WillBeAPlayerScav() && scavRoles.Contains(p.Info.Settings.Role) ? 1 : 0);
            if (scavCount == 0)
            {
                return allowSpawn(scavCount);
            }

            if (NonWavesSpawnScenarioCreatePatch.SpawnedScavs < QuestingBotsPluginConfig.ScavSpawnLimitThreshold.Value)
            {
                return allowSpawn(scavCount);
            }

            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            float raidTimeElapsedFraction = 1f - Helpers.RaidHelpers.GetRaidTimeRemainingFraction();
            int maxScavsAllowedToSpawn = (int)Math.Round(raidTimeElapsedFraction * locationData.MaxTotalBots * QuestingBotsPluginConfig.TotalScavSpawnLimitFraction.Value, 0);

            if (NonWavesSpawnScenarioCreatePatch.SpawnedScavs >= maxScavsAllowedToSpawn)
            {
                Controllers.LoggingController.LogWarning("Prevented " + scavCount + " Scavs from spawning per the Scav spawn rate limiter");
                return false;
            }

            return allowSpawn(scavCount);
        }

        private static bool allowSpawn(int scavCount)
        {
            Controllers.LoggingController.LogWarning("Allowing " + scavCount + " Scavs to spawn per the Scav spawn rate limiter");

            NonWavesSpawnScenarioCreatePatch.AddSpawnedScavs(scavCount);
            return true;
        }
    }
}
