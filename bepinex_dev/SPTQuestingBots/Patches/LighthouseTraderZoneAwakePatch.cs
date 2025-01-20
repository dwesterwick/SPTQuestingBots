using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches
{
    public class LighthouseTraderZoneAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LighthouseTraderZone).GetMethod("Awake", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(PhysicsTriggerHandler ___physicsTriggerHandler_0)
        {
            Components.LightkeeperIslandMonitor.LightkeeperTraderZoneColliderHandler = ___physicsTriggerHandler_0;
            Controllers.LoggingController.LogInfo("Found collider for the Lighthouse trader zone");
        }
    }
}
