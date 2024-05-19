using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;

namespace SPTQuestingBotsInteropTest
{
    public class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            return localGameType.GetMethod("method_18", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref IEnumerator __result, object __instance, float startDelay)
        {
            if (!SPTQuestingBots.QuestingBotsInterop.IsQuestingBotsLoaded())
            {
                LoggingController.LogWarning("Questing Bots not detected");
            }

            if (!SPTQuestingBots.QuestingBotsInterop.Init())
            {
                LoggingController.LogWarning("Questing Bots interop could not be initialized");
            }

            Task.Run(async() => await checkBotGenerators());
        }

        private static async Task checkBotGenerators()
        {
            int remainingGenerators = int.MaxValue;
            while (remainingGenerators > 0)
            {
                SPTQuestingBots.QuestingBotsBotGeneratorStatus generatorStatus = SPTQuestingBots.QuestingBotsInterop.GetBotGeneratorStatus();
                remainingGenerators = generatorStatus.RemainingBotGenerators;

                if (remainingGenerators > 0)
                {
                    LoggingController.LogInfo($"Waiting for Questing Bots to generate {generatorStatus.CurrentBotGeneratorType}s... ({generatorStatus.CurrentBotGeneratorProgress}%)");
                    await Task.Delay(1000);
                }
            }
        }
    }
}
