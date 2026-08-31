using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using QuestingBots.Helpers;

namespace QuestingBots.Patches
{
    internal class PScavProfilePatch : ModulePatch
    {
        private static FieldInfo profileListField = null!;

        protected override MethodBase GetTargetMethod()
        {
            profileListField = AccessTools.Field(typeof(ABotProfileCreator), "_simpleProfiles");

            return typeof(ABotProfileCreator).GetMethod(
                nameof(ABotProfileCreator.GetNewProfile),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(BotCreationData), typeof(bool) },
                null);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(object __instance, ref Profile __result, BotCreationData data, bool withDelete)
        {
            bool shouldSpawnPScav = RaidHelpers.ShouldSpawnPScavByChance();

            List<Profile> cachedProfiles = (List<Profile>)profileListField.GetValue(__instance);
            List<Profile> matchingCachedProfiles = cachedProfiles.ApplyFilter(profile => shouldSpawnPScav ^ !profile.WillBeAPlayerScav());

            __result = matchingCachedProfiles.Count > 0 ? data.ChooseProfile(matchingCachedProfiles, withDelete) : null!;
            return false;
        }
    }
}
