using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches.Spawning
{
    public class AddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsGroup).GetMethod(nameof(BotsGroup.AddEnemy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            // We only care about bot groups adding you as an enemy
            if (!person.IsYourPlayer)
            {
                return true;
            }

            // This only matters in Scav raids
            // TO DO: This might also matter in PMC raids if a mod adds groups that are friendly to the player
            /*if (!Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.IsScavRaid)
            {
                return true;
            }*/

            // Get the ID's of all group members
            List<BotOwner> groupMemberList = SPT.Custom.CustomAI.AiHelpers.GetAllMembers(__instance);
            string[] groupMemberIDs = groupMemberList.Select(m => m.Profile.Id).ToArray();

            //LoggingController.LogInfo("You are now an enemy of " + string.Join(", ", groupMemberIDs) + " due to reason: " + cause.ToString());

            // We only care about one enemy cause
            if (cause != EBotEnemyCause.pmcBossKill)
            {
                return true;
            }

            // Check if the the bot group was created by this mod
            bool isGroupFromBotGenerator = false;
            foreach (Components.Spawning.BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(Components.Spawning.BotGenerator)))
            {
                if (botGenerator.GetBotGroups().Any(g => g.SpawnedBots.Any(b => groupMemberIDs.Contains(b.Profile.Id))))
                {
                    isGroupFromBotGenerator = true;
                    break;
                }
            }

            if (isGroupFromBotGenerator)
            {
                LoggingController.LogWarning("Preventing BotsGroup::AddEnemy from running due to EBotEnemyCause.pmcBossKill because the victim was in a bot group created by this mod");
                return false;

                // TODO: The victim list is updated after this method runs, so this doesn't work. However, I don't think we actually care because you will still
                //       become hostile to Scavs if you attack one even if the target method for this patch is bypassed.
                /*if (!person.Profile.Stats.Eft.Victims.Any(v => groupMemberIDs.Contains(v.ProfileId)))
                {
                    LoggingController.LogWarning("Preventing you from becoming an enemy of a bot group because you didn't kill any of its members");

                    return false;
                }*/
            }

            return true;
        }
    }
}
