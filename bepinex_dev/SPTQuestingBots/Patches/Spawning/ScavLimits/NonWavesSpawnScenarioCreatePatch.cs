using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning.ScavLimits
{
    public class NonWavesSpawnScenarioCreatePatch : ModulePatch
    {
        public static NonWavesSpawnScenario MostRecentNonWavesSpawnScenario { get; private set; } = null;

        private static Dictionary<float, int> spawnedScavTimes = new Dictionary<float, int>();

        public static int TotalSpawnedScavs => spawnedScavTimes.Sum(x => x.Value);

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(NonWavesSpawnScenario)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.HasAllParameterTypes(new Type[] { typeof(AbstractGame) }));

            Controllers.LoggingController.LogInfo("Found method for NonWavesSpawnScenarioCreatePatch: " + methodInfo.Name);

            return methodInfo;
        }

        [PatchPostfix]
        protected static void PatchPostfix(NonWavesSpawnScenario __result)
        {
            MostRecentNonWavesSpawnScenario = __result;
        }

        public static void Clear()
        {
            MostRecentNonWavesSpawnScenario = null;
            spawnedScavTimes.Clear();
        }

        public static void AddSpawnedScavs(int count)
        {
            float elapsedTime = RaidHelpers.GetSecondsSinceSpawning();

            if (spawnedScavTimes.ContainsKey(elapsedTime))
            {
                spawnedScavTimes[elapsedTime] += count;
                return;
            }

            spawnedScavTimes.Add(elapsedTime, count);
        }

        public static int GetSpawnedScavCount(float timeWindow, bool excludeBotsBeforeThreshold)
        {
            float elapsedTimeThreshold = RaidHelpers.GetSecondsSinceSpawning() - timeWindow;

            IEnumerable<KeyValuePair<float, int>> scavsToCheck = spawnedScavTimes;

            if (excludeBotsBeforeThreshold)
            {
                int initialScavs = 0;
                scavsToCheck = scavsToCheck.SkipWhile(x => (initialScavs += x.Value) <= QuestingBotsPluginConfig.ScavSpawnLimitThreshold.Value);
            }

            return scavsToCheck
                .Where(x => x.Key >= elapsedTimeThreshold)
                .Sum(x => x.Value);
        }
    }
}
