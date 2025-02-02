using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches.Spawning
{
    public class TimeHasComeScreenClassChangeStatusPatch : ModulePatch
    {
        private static MatchmakerPlayerControllerClass instance = null;
        private static string previousText = "???";
        private static float? previousProgress = null;
        private static bool isOverridingText = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerPlayerControllerClass)
                .GetMethod(nameof(MatchmakerPlayerControllerClass.UpdateMatchingStatus), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(MatchmakerPlayerControllerClass __instance, string status, float? progress = null)
        {
            if (isOverridingText)
            {
                return;
            }

            instance = __instance;
            previousText = status;
            previousProgress = progress;
        }

        public static void ChangeStatus(string text, float? progress = null)
        {
            checkForInstances();
            changeStatus(text, progress);
        }

        public static void RestorePreviousStatus()
        {
            checkForInstances();
            changeStatus(previousText, previousProgress);
        }

        private static void checkForInstances()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("An instance of " + instance.GetType().Name + " has not been discovered yet");
            }
        }

        private static void changeStatus(string text, float? progress)
        {
            isOverridingText = true;
            instance.UpdateMatchingStatus(text, progress);
            isOverridingText = false;
        }
    }
}
