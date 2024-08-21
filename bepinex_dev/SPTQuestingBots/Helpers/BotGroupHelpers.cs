using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    public static class BotGroupHelpers
    {
        public static IEnumerable<BotOwner> FindZryachiyAndFollowers()
        {
            if (!Singleton<IBotGame>.Instantiated || (Singleton<IBotGame>.Instance.BotsController?.Bots?.BotOwners == null))
            {
                return Enumerable.Empty<BotOwner>();
            }

            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.IsZryachiyOrFollower())
                .Where(b => !b.IsDead);
        }

        public static Player GetPlayer(this IPlayer player)
        {
            IEnumerable<Player> matchingPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList
                .Where(p => p.ProfileId == player?.ProfileId);

            if (matchingPlayers.Count() == 1)
            {
                return matchingPlayers.First();
            }

            return null;
        }

        public static BotOwner GetBotOwner(this IPlayer player)
        {
            IEnumerable<BotOwner> matchingOwners = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.Profile.Id == player?.Profile?.Id);

            if (matchingOwners.Count() == 1)
            {
                return matchingOwners.First();
            }

            return null;
        }

        public static void FormAlliance(this IPlayer iplayer, IPlayer playerToAlly)
        {
            if (iplayer == null)
            {
                return;
            }

            iplayer.GetBotOwner()?.BotsGroup?.FormAlliance(playerToAlly);
        }

        public static void FormAlliance(this BotsGroup playerGroup, IPlayer playerToAlly)
        {
            if ((playerGroup == null) || (playerToAlly == null))
            {
                return;
            }

            IPlayer[] enemyMatches = playerGroup.Enemies
                    .Where(e => e.Key.Profile.Id == playerToAlly.Profile.Id)
                    .Select(e => e.Key)
                    .ToArray();

            List<BotOwner> groupMemberList = SPT.Custom.CustomAI.AiHelpers.GetAllMembers(playerGroup);
            string groupMembersText = string.Join(", ", groupMemberList.Select(m => m.GetText()));

            foreach (IPlayer remainingEnemy in enemyMatches)
            {
                LoggingController.LogInfo("Group containing " + groupMembersText + " has paused their hostility with " + remainingEnemy.GetText());
                playerGroup.RemoveEnemy(remainingEnemy);
            }

            Player otherPlayer = playerToAlly.GetPlayer();
            if (!playerGroup.Allies.Contains(otherPlayer))
            {
                LoggingController.LogInfo("Group containing " + groupMembersText + " is temporarily allied with " + otherPlayer.GetText());
                playerGroup.AddAlly(otherPlayer);
            }
        }
    }
}
