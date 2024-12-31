using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Audio.SpatialSystem;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class ProcessSourceOcclusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SpatialAudioSystem).GetMethod(
                "ProcessSourceOcclusion",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(IPlayer), typeof(BetterSource), typeof(bool) },
                null);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(IPlayer player, BetterSource source, bool allowedLimiter)
        {
            if (source == null)
            {
                LoggingController.LogWarning("Skipping ProcessSourceOcclusion with null sound for " + player.GetText() + "...");

                return false;
            }

            return true;
        }
    }
}
