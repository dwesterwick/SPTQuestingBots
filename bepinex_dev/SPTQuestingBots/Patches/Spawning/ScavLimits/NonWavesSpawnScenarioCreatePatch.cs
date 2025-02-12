using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Spawning.ScavLimits
{
    public class NonWavesSpawnScenarioCreatePatch : ModulePatch
    {
        public static NonWavesSpawnScenario MostRecentNonWavesSpawnScenario { get; private set; } = null;

        private static Dictionary<float, int> spawnedScavTimes = new Dictionary<float, int>();

        public static int TotalSpawnedScavs => spawnedScavTimes.Sum(x => x.Value);

        protected override MethodBase GetTargetMethod()
        {
            return typeof(NonWavesSpawnScenario).GetMethod("smethod_0", BindingFlags.Public | BindingFlags.Static);
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
            float elapsedTime = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();

            if (spawnedScavTimes.ContainsKey(elapsedTime))
            {
                spawnedScavTimes[elapsedTime] += count;
                return;
            }

            spawnedScavTimes.Add(elapsedTime, count);
        }

        public static int GetSpawnedScavCount(float timeWindow, bool excludeBotsBeforeThreshold)
        {
            float elapsedTime = SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();
            float elapsedTimeThreshold = elapsedTime - timeWindow;

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
