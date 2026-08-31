using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using QuestingBots.Utils;
using SPT.Reflection.Patching;

namespace QuestingBots.Patches.Lighthouse
{
    public class LighthouseTraderZoneAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LighthouseTraderZone).GetMethod(nameof(LighthouseTraderZone.Awake), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(PhysicsTriggerHandler ____triggerHandler)
        {
            Components.LightkeeperIslandMonitor.LightkeeperTraderZoneColliderHandler = ____triggerHandler;
            Singleton<LoggingUtil>.Instance.LogDebug("Found collider for the Lighthouse trader zone");
        }
    }
}
