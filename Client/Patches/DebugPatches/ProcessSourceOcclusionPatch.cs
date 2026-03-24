using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Audio.SpatialSystem;
using EFT;
using SPT.Reflection.Patching;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Patches.DebugPatches
{
    public class ProcessSourceOcclusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SpatialAudioSystem).GetMethod(
                nameof(SpatialAudioSystem.ProcessSourceOcclusion),
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
                Singleton<LoggingUtil>.Instance.LogWarning("Skipping ProcessSourceOcclusion with null sound for " + player.GetText() + "...");

                return false;
            }

            return true;
        }
    }
}
