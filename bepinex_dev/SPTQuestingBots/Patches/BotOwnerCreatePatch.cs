using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotOwner __result)
        {
            if ((__result.Profile.Info.Settings.Role != WildSpawnType.pmcBEAR) && (__result.Profile.Info.Settings.Role != WildSpawnType.pmcBEAR))
            {
                return;
            }

            FieldInfo followersField = AccessTools.Field(typeof(BotBoss), "_followers");
            var ____followers = followersField.GetValue(__result.Boss);

            Controllers.LoggingController.LogInfo("Followers type: " + ____followers.GetType().Name);

            followersField.SetValue(__result.Boss, new GClass430(__result));
            ____followers = followersField.GetValue(__result.Boss);

            Controllers.LoggingController.LogInfo("Followers type after update: " + ____followers.GetType().Name);
        }
    }
}
