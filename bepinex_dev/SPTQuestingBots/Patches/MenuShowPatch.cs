using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
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

            if (!_displayedReflexWarning && ConfigController.Config.Enabled && ConfigController.Config.BotSpawns.Enabled)
            {
                string profileWarningMessage = "Using nVidia Reflex may result in long raid loading times";
                NotificationManagerClass.DisplayWarningNotification(profileWarningMessage, EFT.Communications.ENotificationDurationType.Long);
                LoggingController.LogWarningToServerConsole(profileWarningMessage);

                _displayedReflexWarning = true;
            }
        }
    }
}
