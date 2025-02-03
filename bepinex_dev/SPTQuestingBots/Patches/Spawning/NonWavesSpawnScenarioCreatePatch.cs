using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Spawning
{
    public class NonWavesSpawnScenarioCreatePatch : ModulePatch
    {
        public static NonWavesSpawnScenario MostRecentNonWavesSpawnScenario { get; private set; } = null;
        public static int SpawnedScavs { get; private set; } = 0;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(NonWavesSpawnScenario).GetMethod("smethod_0", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        protected static void PatchPostfix(NonWavesSpawnScenario __result)
        {
            Controllers.LoggingController.LogInfo("Updating the current NonWavesSpawnScenario instance");

            MostRecentNonWavesSpawnScenario = __result;
        }

        public static void Clear()
        {
            Controllers.LoggingController.LogInfo("The current NonWavesSpawnScenario instance is stopping");

            MostRecentNonWavesSpawnScenario = null;
            SpawnedScavs = 0;
        }

        public static void AddSpawnedScavs(int count)
        {
            SpawnedScavs += count;
        }
    }
}
