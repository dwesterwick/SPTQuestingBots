using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches
{
    internal class PScavProfilePatch : ModulePatch
    {
        private static Type targetType;
        private static FieldInfo profileListField;

        protected override MethodBase GetTargetMethod()
        {
            targetType = typeof(BotsPresets).BaseType;
            profileListField = AccessTools.Field(targetType, "list_0");

            Controllers.LoggingController.LogInfo("Found type for ServerRequestPatch: " + targetType.FullName);

            return targetType.GetMethod("GetNewProfile", new Type[] { typeof(BotCreationDataClass), typeof(bool) });
        }

        [PatchPrefix]
        protected static bool PatchPrefix(object __instance, ref Profile __result, BotCreationDataClass data, bool withDelete)
        {
            bool shouldSpawnPScav = RaidHelpers.ShouldSpawnPScavByChance();

            List<Profile> cachedProfiles = (List<Profile>)profileListField.GetValue(__instance);
            List<Profile> matchingCachedProfiles = cachedProfiles.ApplyFilter(profile => shouldSpawnPScav ^ !profile.WillBeAPlayerScav());

            __result = matchingCachedProfiles.Count > 0 ? data.ChooseProfile(matchingCachedProfiles, withDelete) : null;

            return false;
        }
    }
}
