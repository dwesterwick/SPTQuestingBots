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
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(IAssetsManager assetsManager, InputTree inputTree)
        {
            if (!Helpers.VersionCheckHelper.CheckSPTVersion())
            {
                Chainloader.DependencyErrors.Add("Could not load " + QuestingBotsPlugin.ModName + " because it requires a newer SPT version");
                return;
            }
        }
    }
}
