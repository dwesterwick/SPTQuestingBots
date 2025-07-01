using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBotsInteropTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots_InteropTest
{
    public class BotsControllerSetSettingsPatch : ModulePatch
    {
        private const int UPDATE_DELAY = 5000;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.SetSettings), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (!SPTQuestingBots.QuestingBotsInterop.IsQuestingBotsLoaded())
            {
                LoggingController.LogWarning("Questing Bots not detected");
                return;
            }

            if (!SPTQuestingBots.QuestingBotsInterop.Init())
            {
                LoggingController.LogWarning("Questing Bots interop could not be initialized");
                return;
            }

            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<BotDecisionLoggingComponent>();
        }
    }
}
