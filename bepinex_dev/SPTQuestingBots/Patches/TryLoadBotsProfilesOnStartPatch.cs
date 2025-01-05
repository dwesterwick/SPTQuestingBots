using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        public static List<Task<Profile[]>> GenerateBotsTasks { get; private set; } = new List<Task<Profile[]>>();

        public static int RemainingBotGenerationTasks => GenerateBotsTasks.Count(t => !t.IsCompleted);

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsPresets).GetMethod("method_2", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix(List<WaveInfo> waves, EProfilesAskingStat stat)
        {
            LoggingController.LogInfo("Found Task for generating " + waves.Count + " bot preset waves");

            if (ConfigController.Config.BotSpawns.Enabled)
            {
                // Ensure the PMC-conversion chances have remained at 0%
                ConfigController.AdjustPMCConversionChances(0, true);
            }
        }

        [PatchPostfix]
        protected static void PatchPostfix(Task<Profile[]> __result, List<WaveInfo> waves, EProfilesAskingStat stat)
        {
            GenerateBotsTasks.Add(__result);
        }
    }
}
