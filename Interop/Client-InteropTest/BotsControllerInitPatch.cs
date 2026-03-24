using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using QuestingBotsInteropTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots_InteropTest
{
    public class BotsControllerInitPatch : ModulePatch
    {
        private const int UPDATE_DELAY = 5000;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.Init), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (!QuestingBots.QuestingBotsInterop.IsQuestingBotsLoaded())
            {
                LoggingController.LogWarning("Questing Bots not detected");
                return;
            }

            if (!QuestingBots.QuestingBotsInterop.Init())
            {
                LoggingController.LogWarning("Questing Bots interop could not be initialized");
                return;
            }

            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<BotDecisionLoggingComponent>();
        }
    }
}
