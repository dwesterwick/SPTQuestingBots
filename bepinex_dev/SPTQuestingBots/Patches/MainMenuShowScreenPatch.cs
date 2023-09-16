using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.UI;

namespace SPTQuestingBots.Patches
{
    public class MainMenuShowScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("ShowScreen", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(EMenuType screen, bool turnOn)
        {
            Controllers.LocationController.CacheEscapeTimes();
        }
    }
}
