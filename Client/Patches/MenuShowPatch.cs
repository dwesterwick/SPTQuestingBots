using Comfort.Common;
using EFT.Communications;
using EFT.UI;
using QuestingBots.ExternalMods;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class MenuShowPatch : ModulePatch
    {
        private static bool _displayedReflexWarning = false;
        private static bool _displayedFikaWarning = false;

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

            if (!_displayedFikaWarning && fikaInstalledWithoutSyncPlugin())
            {
                string message = "You must use " + ExternalModHandler.QuestingBotsFikaSyncModInfo.Name + " when using Fika or spawn-rush quests will be disabled and key spawning will not propogate to client machines when bots unlock doors!";
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(message);

                message = "Missing Questing Bots Fika sync plugin";
                NotificationManager.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Long);

                _displayedFikaWarning = true;
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
            NotificationManager.DisplayWarningNotification(profileWarningMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogWarningToServerConsole(profileWarningMessage);

            _displayedReflexWarning = true;
        }

        private static bool fikaInstalledWithoutSyncPlugin()
        {
            if (!ExternalModHandler.FikaModInfo.IsInstalled)
            {
                return false;
            }

            if (ExternalModHandler.QuestingBotsFikaSyncModInfo.IsInstalled)
            {
                return false;
            }

            return true;
        }
    }
}
