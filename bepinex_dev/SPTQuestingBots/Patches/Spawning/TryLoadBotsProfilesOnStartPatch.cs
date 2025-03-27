using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning
{
    public class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        public static List<Task<Profile[]>> GenerateBotsTasks { get; private set; } = new List<Task<Profile[]>>();

        public static int RemainingBotGenerationTasks => GenerateBotsTasks.Count(t => !t.IsCompleted);

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo[] matchingMethods = typeof(BotsPresets)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.HasAllParameterTypesInOrder(new Type[] { typeof(List<WaveInfoClass>), typeof(EProfilesAskingStat) }))
                .ToArray();

            if (matchingMethods.Length != 1)
            {
                throw new TypeLoadException("Could not find matching method for TryLoadBotsProfilesOnStartPatch");
            }

            LoggingController.LogInfo("Found method for TryLoadBotsProfilesOnStartPatch: " + matchingMethods[0].Name);

            return matchingMethods[0];
        }

        [PatchPrefix]
        protected static void PatchPrefix(List<WaveInfoClass> waves, EProfilesAskingStat stat)
        {
            if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                LoggingController.LogInfo("Found Task for generating " + waves.Count + " bot preset waves");
            }
        }

        [PatchPostfix]
        protected static void PatchPostfix(Task<Profile[]> __result, List<WaveInfoClass> waves, EProfilesAskingStat stat)
        {
            GenerateBotsTasks.Add(__result);
        }
    }
}
