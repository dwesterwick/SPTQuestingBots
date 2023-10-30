using Aki.Reflection.Patching;
using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class IsFollowerSuitableForBossPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotBoss).GetMethod("OfferSelf", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref bool __result, BotBoss __instance, BotOwner offer)
        {
            if (__instance.Owner.Profile.Id == offer.Profile.Id)
            {
                return true;
            }

            //Controllers.LoggingController.LogInfo("Checking if " + offer.Profile.Nickname + " can be a follower for " + __instance.Owner.Profile.Nickname + "...");

            IEnumerable<BotOwner> groupMembers;
            if (BotGenerator.SpawnedInitialPMCCount > 0)
            {
                if (!BotGenerator.IsBotFromInitialPMCSpawns(offer))
                {
                    return true;
                }

                groupMembers = BotGenerator.GetSpawnGroupMembers(__instance.Owner);
            }
            else
            {
                List<BotOwner> groupMemberList = new List<BotOwner>();
                for (int m = 0; m < offer.BotsGroup.MembersCount; m++)
                {
                    groupMemberList.Add(offer.BotsGroup.Member(m));
                }

                groupMembers = groupMemberList;
            }

            //Controllers.LoggingController.LogInfo(__instance.Owner.Profile.Nickname + "'s group contains: " + string.Join(",", groupMembers.Select(m => m.Profile.Nickname)));

            if (!groupMembers.Any(m => m.Id == offer.Id))
            {
                Controllers.LoggingController.LogInfo("Preventing " + offer.Profile.Nickname + " from becoming a follower for " + __instance.Owner.Profile.Nickname);

                __result = false;
                return false;
            }

            return true;
        }
    }
}
