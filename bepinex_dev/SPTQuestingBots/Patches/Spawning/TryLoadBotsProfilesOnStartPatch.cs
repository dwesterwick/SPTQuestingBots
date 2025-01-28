using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches.Spawning
{
    public class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        public static List<Task<Profile[]>> GenerateBotsTasks { get; private set; } = new List<Task<Profile[]>>();

        private static bool checkedPMCConversionChance = false;

        public static int RemainingBotGenerationTasks => GenerateBotsTasks.Count(t => !t.IsCompleted);

        protected override MethodBase GetTargetMethod()
        {
            MethodInfo[] matchingMethods = typeof(BotsPresets)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetParameters().Length == 2)
                .Where(m => m.GetParameters()[0].ParameterType == typeof(List<WaveInfo>))
                .Where(m => m.GetParameters()[1].ParameterType == typeof(EProfilesAskingStat))
                .ToArray();

            if (matchingMethods.Length != 1)
            {
                throw new TypeLoadException("Could not find matching method for TryLoadBotsProfilesOnStartPatch");
            }

            LoggingController.LogInfo("Found method for TryLoadBotsProfilesOnStartPatch: " + matchingMethods[0].Name);

            return matchingMethods[0];
        }

        [PatchPrefix]
        protected static void PatchPrefix(List<WaveInfo> waves, EProfilesAskingStat stat)
        {
            if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                LoggingController.LogInfo("Found Task for generating " + waves.Count + " bot preset waves");
            }

            if (!checkedPMCConversionChance && ConfigController.Config.BotSpawns.Enabled)
            {
                // Ensure the PMC-conversion chances have remained at 0%
                ConfigController.AdjustPMCConversionChances(0, true);
            }

            checkedPMCConversionChance = true;
        }

        [PatchPostfix]
        protected static void PatchPostfix(Task<Profile[]> __result, List<WaveInfo> waves, EProfilesAskingStat stat)
        {
            GenerateBotsTasks.Add(__result);
        }
    }
}
