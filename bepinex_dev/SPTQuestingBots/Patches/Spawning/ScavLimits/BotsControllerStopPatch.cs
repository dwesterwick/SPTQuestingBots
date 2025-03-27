using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Spawning.ScavLimits
{
    public class BotsControllerStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.StopGettingInfo), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix()
        {
            NonWavesSpawnScenarioCreatePatch.Clear();
        }
    }
}
