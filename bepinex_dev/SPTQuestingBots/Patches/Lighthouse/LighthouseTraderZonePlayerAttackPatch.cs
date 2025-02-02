using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Lighthouse
{
    public class LighthouseTraderZonePlayerAttackPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(LighthouseTraderZone)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m => m.HasAllParameterTypes(new Type[] { typeof(DamageInfoStruct) }));

            LoggingController.LogInfo("Found method for LighthouseTraderZonePlayerAttackPatch: " + methodInfo.Name);

            return methodInfo;
        }

        [PatchPostfix]
        protected static void PatchPostfix(Player player, IPlayer lastAgressor, PhysicsTriggerHandler ___physicsTriggerHandler_0)
        {
            if (player == null || lastAgressor == null)
            {
                return;
            }

            if (player.Profile.Id == lastAgressor.Profile.Id)
            {
                return;
            }

            if (lastAgressor.IsAI)
            {
                return;
            }

            // The victim already killed another player on the island
            if (player.IsAgressorInLighthouseTraderZone)
            {
                return;
            }

            // Ignore victims that are not on the island
            if (!___physicsTriggerHandler_0.trigger.bounds.Contains(player.Position))
            {
                LoggingController.LogWarning("[DSP Not Changed] Victim not on the island");
                return;
            }

            // If the aggressor doesn't have a DSP, there's nothing to do
            Player lastAgressorPlayer = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(lastAgressor.ProfileId);
            if (!lastAgressorPlayer.HasAGreenOrYellowDSP())
            {
                LoggingController.LogWarning("[DSP Not Changed] Aggressor does not have an encoded DSP.");
                return;
            }
            
            LoggingController.LogInfo(lastAgressorPlayer.GetText() + " attacked " + player.GetText() + " on Lightkeeper Island. Updating their DSP...");
            if (!lastAgressorPlayer.TryReduceLightkeeperStanding())
            {
                LoggingController.LogError("Could not update " + lastAgressorPlayer.GetText() + "'s DSP");
            }
        }
    }
}
