using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;
using SPT.Reflection.Patching;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Patches
{
    public class MenuShowPatch : ModulePatch
    {
        private static bool _displayedReflexWarning = false;

        protected override MethodBase GetTargetMethod()
        {
            // Same as SPT method to display plugin errors
            return typeof(MenuScreen).GetMethods().First(m => m.Name == nameof(MenuScreen.Show));
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            if (shouldShowNvidiaReflexWarning())
            {
                showNvidiaReflexWarning();
            }
        }

        private static bool shouldShowNvidiaReflexWarning()
        {
            if (_displayedReflexWarning)
            {
                return false;
            }

            // This is only an issue when using the Queting Bots spawning system
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Enabled || !Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return false;
            }

            if (!GameCompatibilityCheckHelper.IsNvidiaReflexEnabled())
            {
                _displayedReflexWarning = false;
                return false;
            }

            return true;
        }

        private static void showNvidiaReflexWarning()
        {
            string profileWarningMessage = "Using nVidia Reflex may result in long raid loading times";
            NotificationManagerClass.DisplayWarningNotification(profileWarningMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogWarningToServerConsole(profileWarningMessage);

            _displayedReflexWarning = true;
        }
    }
}
