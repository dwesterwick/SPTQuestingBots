using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.UI.Matchmaker;
using TMPro;

namespace SPTQuestingBots.Patches
{
    public class MatchmakerFinalCountdownUpdatePatch : ModulePatch
    {
        private static string text = null;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchmakerFinalCountdown).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(TextMeshProUGUI ____time)
        {
            if (text == null)
            {
                return true;
            }

            ____time.SetText(text);
            return false;
        }

        public static void ResetText()
        {
            text = null;
        }

        public static void SetText(string _text)
        {
            text = _text;
        }
    }
}
