using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots.Spawning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class GameWorldCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocalPlayer).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (ConfigController.Config.InitialPMCSpawns.Enabled)
            {
                PMCGenerator pmcGenerator = Singleton<GameWorld>.Instance.gameObject.AddComponent<PMCGenerator>();
                Singleton<PMCGenerator>.Create(pmcGenerator);
            }
        }
    }
}
