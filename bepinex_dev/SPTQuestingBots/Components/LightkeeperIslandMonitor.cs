using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Components
{
    public class LightkeeperIslandMonitor : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        private LocationData locationData;
        private List<Player> playersOnIsland = new List<Player>();
        private List<BotOwner> botsWithQuestsOnIsland = new List<BotOwner>();
        private Dictionary<Player, IPlayer[]> originalAllies = new Dictionary<Player, IPlayer[]>();
        private Dictionary<Player, IPlayer[]> originalEnemies = new Dictionary<Player, IPlayer[]>();

        private void Awake()
        {
            locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();
        }

        private void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                IsPlayerOnLightkeeperIsland(player);
            }
        }

        public IEnumerable<BotOwner> FindZryachiyAndFollowers()
        {
            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => IsZryachiyOrFollower(b.Profile));
        }

        public bool IsZryachiyOrFollower(Profile profile)
        {
            return (profile.Info.Settings.Role == WildSpawnType.bossZryachiy) || (profile.Info.Settings.Role == WildSpawnType.followerZryachiy);
        }

        public bool IsPlayerOnLightkeeperIsland(Player player)
        {
            bool isOnIsland = locationData.IsPointOnLightkeeperIsland(player.Position);

            if (IsZryachiyOrFollower(player.Profile))
            {
                foreach (BotOwner botWithQuestsOnIsland in botsWithQuestsOnIsland)
                {
                    formAlliancesWithZryachiyAndFollowers(botWithQuestsOnIsland);
                }
            }

            if (playersOnIsland.Contains(player) != isOnIsland)
            {
                ToggleHostility(player);
            }

            return isOnIsland;
        }

        public bool IsBotObjectiveOnLightkeeperIsland(BotOwner bot)
        {
            BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);
            if (botObjectiveManager == null)
            {
                return false;
            }

            Vector3? assignmentPosition = botObjectiveManager.Position;
            if (!assignmentPosition.HasValue)
            {
                return false;
            }

            bool isQuestOnIsland = locationData.IsPointOnLightkeeperIsland(assignmentPosition.Value);
            if (isQuestOnIsland)
            {
                botsWithQuestsOnIsland.Add(bot);
                formAlliancesWithZryachiyAndFollowers(bot);
            }

            return isQuestOnIsland;
        }

        private void formAlliancesWithZryachiyAndFollowers(BotOwner bot)
        {
            foreach (BotOwner zryachiyOrFollower in FindZryachiyAndFollowers())
            {
                if (bot.BotsGroup.Allies.Contains(zryachiyOrFollower.GetPlayer))
                {
                    continue;
                }

                bot.BotsGroup.AddAlly(zryachiyOrFollower.GetPlayer);
                LoggingController.LogInfo(bot.GetText() + "'s group is now allied with " + zryachiyOrFollower.GetText() + " because they have a quest on Lightkeeper Island");
            }
        }

        private void ToggleHostility(Player player)
        {
            if (playersOnIsland.Contains(player))
            {
                LoggingController.LogInfo(player.GetText() + " has left Lightkeeper Island");
                playersOnIsland.Remove(player);

                RevertAlliances(player);
                foreach (Player otherPlayer in playersOnIsland)
                {
                    if (player.Profile.Id == otherPlayer.Profile.Id)
                    {
                        continue;
                    }

                    RevertAlliances(otherPlayer, player);
                }
            }
            else
            {
                LoggingController.LogInfo(player.GetText() + " has entered Lightkeeper Island");
                playersOnIsland.Add(player);

                CreateTemporaryAlliances(player);
            }
        }

        private void CreateTemporaryAlliances(Player player)
        {
            SetOriginalAllies(player);
            SetOriginalEnemies(player);

            BotsGroup playerGroup = getBotOwnerForPlayer(player)?.BotsGroup;

            foreach (Player otherPlayer in playersOnIsland)
            {
                if (player.Profile.Id == otherPlayer.Profile.Id)
                {
                    continue;
                }

                BotsGroup otherPlayerGroup = getBotOwnerForPlayer(otherPlayer)?.BotsGroup;

                if ((playerGroup != null) && playerGroup.Enemies.ContainsKey(otherPlayer))
                {
                    LoggingController.LogInfo(player.GetText() + "'s group has paused their hostility with " + otherPlayer.GetText());
                    playerGroup.RemoveEnemy(otherPlayer);
                }

                if ((otherPlayerGroup != null) && otherPlayerGroup.Enemies.ContainsKey(player))
                {
                    LoggingController.LogInfo(otherPlayer.GetText() + "'s group has paused their hostility with " + player.GetText());
                    otherPlayerGroup.RemoveEnemy(player);
                }

                if ((playerGroup != null) && !playerGroup.Allies.Contains(otherPlayer))
                {
                    LoggingController.LogInfo(player.GetText() + "'s group is temporarily allied with " + otherPlayer.GetText());
                    playerGroup.AddAlly(otherPlayer);
                }

                if ((otherPlayerGroup != null) && !otherPlayerGroup.Allies.Contains(player))
                {
                    LoggingController.LogInfo(otherPlayer.GetText() + "'s group is temporarily allied with " + player.GetText());
                    otherPlayerGroup.AddAlly(player);
                }
            }
        }

        private void RevertAlliances(Player player, Player otherPlayer = null)
        {
            BotsGroup playerGroup = getBotOwnerForPlayer(player)?.BotsGroup;
            if (playerGroup == null)
            {
                return;
            }

            foreach (IPlayer ally in playerGroup.Allies.ToArray())
            {
                if ((otherPlayer != null) && (ally.Profile.Id != otherPlayer.Profile.Id))
                {
                    continue;
                }

                if (!originalAllies[player].Contains(ally))
                {
                    LoggingController.LogInfo(player.GetText() + "'s group is no longer allied with " + ally.GetText());

                    BotOwner allyOwner = getBotOwnerForPlayer(ally);
                    if (allyOwner != null)
                    {
                        playerGroup.RemoveAlly(allyOwner);
                    }

                    playerGroup.Allies.Remove(ally);
                }
            }

            foreach (IPlayer enemy in originalEnemies[player])
            {
                if ((otherPlayer != null) && (enemy.Profile.Id != otherPlayer.Profile.Id))
                {
                    continue;
                }

                if (!playerGroup.Enemies.ContainsKey(enemy))
                {
                    LoggingController.LogInfo(player.GetText() + "'s group has restored their hostility with " + enemy.GetText());
                    playerGroup.AddEnemy(enemy, EBotEnemyCause.initCauseEnemy);
                }
            }
        }

        private void SetOriginalAllies(Player player)
        {
            BotsGroup playerGroup = getBotOwnerForPlayer(player)?.BotsGroup;
            if (playerGroup == null)
            {
                return;
            }

            IPlayer[] allies = playerGroup.Allies.ToArray();

            if (originalAllies.ContainsKey(player))
            {
                originalAllies[player] = allies;
            }
            else
            {
                originalAllies.Add(player, allies);
            }
        }

        private void SetOriginalEnemies(Player player)
        {
            BotsGroup playerGroup = getBotOwnerForPlayer(player)?.BotsGroup;
            if (playerGroup == null)
            {
                return;
            }

            IPlayer[] enemies = playerGroup.Enemies.Keys.ToArray();

            if (originalEnemies.ContainsKey(player))
            {
                originalEnemies[player] = enemies;
            }
            else
            {
                originalEnemies.Add(player, enemies);
            }
        }

        private BotOwner getBotOwnerForPlayer(IPlayer player)
        {
            IEnumerable<BotOwner> matchingOwners = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.Profile.Id == player.Profile.Id);

            if (matchingOwners.Count() == 1)
            {
                return matchingOwners.First();
            }

            return null;
        }
    }
}
