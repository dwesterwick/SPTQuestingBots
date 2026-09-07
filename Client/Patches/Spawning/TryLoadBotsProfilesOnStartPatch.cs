using EFT;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches.Spawning
{
    public class TryLoadBotsProfilesOnStartPatch : ModulePatch
    {
        public static List<Task<Profile[]>> GenerateBotsTasks { get; private set; } = new List<Task<Profile[]>>();

        public static int RemainingBotGenerationTasks => GenerateBotsTasks.Count(t => !t.IsCompleted);

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotProfileClient).GetMethod(nameof(BotProfileClient.LoadProfiles), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(Task<Profile[]> __result, List<CountTypeBotWave> waves, EProfilesAskingStat stat)
        {
            GenerateBotsTasks.Add(__result);
        }
    }
}
