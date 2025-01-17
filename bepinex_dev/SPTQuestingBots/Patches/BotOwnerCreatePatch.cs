using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerCreatePatch : ModulePatch
    {
        private static FieldInfo followersField;

        protected override MethodBase GetTargetMethod()
        {
            followersField = AccessTools.Field(typeof(BotBoss), "_followers");

            return typeof(BotOwner).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotOwner __result)
        {
            if (!__result.WillBeAPMC())
            {
                return;
            }

            // This needs to be set to an instance of the base "followers" class or bosses will not regularly search for new followers
            followersField.SetValue(__result.Boss, new GClass430(__result));
        }
    }
}
