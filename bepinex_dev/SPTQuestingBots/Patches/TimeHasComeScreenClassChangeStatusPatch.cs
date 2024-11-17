using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches
{
    public class TimeHasComeScreenClassChangeStatusPatch : ModulePatch
    {
        private static MatchmakerTimeHasCome.TimeHasComeScreenClass instance = null;
        private static string previousText = "???";
        private static float? previousProgress = null;
        private static bool isOverridingText = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerTimeHasCome.TimeHasComeScreenClass).GetMethod("ChangeStatus", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(MatchmakerTimeHasCome.TimeHasComeScreenClass __instance, string text, float? progress = null)
        {
            if (isOverridingText)
            {
                return;
            }

            instance = __instance;
            previousText = text;
            previousProgress = progress;
        }

        public static void ChangeStatus(string text, float? progress = null)
        {
            checkForInstance();

            isOverridingText = true;
            instance.ChangeStatus(text, progress);
            isOverridingText = false;
        }

        public static void RestorePreviousStatus()
        {
            checkForInstance();

            isOverridingText = true;
            instance.ChangeStatus(previousText, previousProgress);
            isOverridingText = false;
        }

        private static void checkForInstance()
        {
            if (instance == null)
            {
                throw new InvalidOperationException("An instance of MatchmakerTimeHasCome.TimeHasComeScreenClass has not been discovered yet");
            }
        }
    }
}
