using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using BepInEx.Bootstrap;
using EFT;
using EFT.InputSystem;

namespace SPTQuestingBots.Patches
{
    public class CheckSPTVersionPatch : ModulePatch
    {
        public static string MinVersion { get; set; } = "0.0.0.0";
        public static string MaxVersion { get; set; } = "999999.999999.999999.999999";

        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(IAssetsManager assetsManager, InputTree inputTree)
        {
            if (!Helpers.VersionCheckHelper.IsSPTWithinVersionRange(MinVersion, MaxVersion, out string currentVersion))
            {
                string errorMessage = "Could not load " + QuestingBotsPlugin.ModName + " because it requires SPT-AKI ";
                
                if (MinVersion == MaxVersion)
                {
                    errorMessage += MinVersion;
                }
                else if (MaxVersion == "999999.999999.999999.999999")
                {
                    errorMessage += MinVersion + " or later";
                }
                else if (MinVersion == "0.0.0.0")
                {
                    errorMessage += MaxVersion + " or older";
                }
                else
                {
                    errorMessage += "between versions " + MinVersion + " and " + MaxVersion;
                }

                errorMessage += ". The current version is " + currentVersion + ".";

                Chainloader.DependencyErrors.Add(errorMessage);
            }
        }
    }
}
