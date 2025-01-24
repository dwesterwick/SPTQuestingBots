using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class MenuShowPatch : ModulePatch
    {
        private static bool _displayedReflexWarning = false;
        private static bool _displayedPerformanceImprovementsError = false;
        private static bool _displayedPleaseJustFightWarning = false;
        private static string pleaseJustFightGuid = "Shibdib.PleaseJustFight";

        protected override MethodBase GetTargetMethod()
        {
            // Same as SPT method to display plugin errors
            return typeof(MenuScreen).GetMethods().First(m => m.Name == nameof(MenuScreen.Show));
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            checkNvidiaReflex();

            if (!_displayedPerformanceImprovementsError && !Helpers.VersionCheckHelper.IsPerformanceImprovementsVersionCompatible() && ConfigController.Config.Enabled)
            {
                string message = "Performance Improvements versions 0.2.1 - 0.2.3 are not compatible with Questing Bots. Please downgrade Performance Improvements to 0.2.0 or remove it to use Questing Bots.";
                LoggingController.LogErrorToServerConsole(message);

                message = "Incompatible version of Performance Improvements detected";
                NotificationManagerClass.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Infinite);

                _displayedPerformanceImprovementsError = true;
            }

            if (!_displayedPleaseJustFightWarning && shouldDisplayPleaseJustFightWarning())
            {
                string message = "Please remove \"Please Just Fight\" while using the Questing Bots spawning system";
                LoggingController.LogErrorToServerConsole(message);

                message = "\"Please Just Fight\" not compatible with QB spawning system";
                NotificationManagerClass.DisplayWarningNotification(message, EFT.Communications.ENotificationDurationType.Long);

                _displayedPleaseJustFightWarning = true;
            }
        }

        private static void checkNvidiaReflex()
        {
            if (_displayedReflexWarning)
            {
                return;
            }

            if (!Singleton<SharedGameSettingsClass>.Instantiated)
            {
                return;
            }

            EFT.Settings.Graphics.ENvidiaReflexMode reflexMode = Singleton<SharedGameSettingsClass>.Instance.Graphics.Settings.NVidiaReflex;
            if (reflexMode == EFT.Settings.Graphics.ENvidiaReflexMode.Off)
            {
                _displayedReflexWarning = false;
                return;
            }

            if (ConfigController.Config.Enabled && ConfigController.Config.BotSpawns.Enabled)
            {
                string profileWarningMessage = "Using nVidia Reflex may result in long raid loading times";
                NotificationManagerClass.DisplayWarningNotification(profileWarningMessage, EFT.Communications.ENotificationDurationType.Long);
                LoggingController.LogWarningToServerConsole(profileWarningMessage);

                _displayedReflexWarning = true;
            }
        }

        private static bool shouldDisplayPleaseJustFightWarning()
        {
            if (_displayedPleaseJustFightWarning)
            {
                return false;
            }

            if (!ConfigController.Config.Enabled || !ConfigController.Config.BotSpawns.Enabled)
            {
                return false;
            }

            if (Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID == pleaseJustFightGuid))
            {
                return true;
            }

            return false;
        }
    }
}
