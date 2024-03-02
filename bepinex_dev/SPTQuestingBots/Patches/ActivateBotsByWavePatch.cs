using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace SPTQuestingBots.Patches
{
    public class ActivateBotsByWavePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(
                "ActivateBotsByWave",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(BossLocationSpawn) },
                null);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BossLocationSpawn wave)
        {
            if (!GameStartPatch.IsDelayingGameStart)
            {
                //LoggingController.LogInfo("Allowing spawn of boss wave " + wave.BossName + "...");
                return true;
            }

            GameStartPatch.AddMissedBossWave(wave);
            //LoggingController.LogInfo("Delaying spawn of boss wave " + wave.BossName + "...");

            return false;
        }
    }
}
