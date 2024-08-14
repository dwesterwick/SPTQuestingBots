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
                .Where(b => IsZryachiyOrFollower(b.Profile))
                .Where(b => !b.IsDead);
        }

        public bool IsZryachiyOrFollower(Profile profile)
        {
            return (profile.Info.Settings.Role == WildSpawnType.bossZryachiy) || (profile.Info.Settings.Role == WildSpawnType.followerZryachiy);
        }

        public bool ShouldPlayerBeFriendlyWithZryachiyAndFollowers(IPlayer iplayer)
        {
            BotOwner botOwner = iplayer.GetBotOwner();
            return playersOnIsland.Contains(iplayer) || ((botOwner != null) && botsWithQuestsOnIsland.Contains(botOwner));
        }

        public bool ShouldGroupBeFriendlyWithZryachiyAndFollowers(BotsGroup group)
        {
            return playersOnIsland.Any(p => p.GetBotOwner()?.BotsGroup == group) || botsWithQuestsOnIsland.Any(b => b.BotsGroup == group);
        }

        public bool IsPlayerOnLightkeeperIsland(Player player)
        {
            bool isOnIsland = locationData.IsPointOnLightkeeperIsland(player.Position);

            if (IsZryachiyOrFollower(player.Profile))
            {
                foreach (BotOwner botWithQuestsOnIsland in botsWithQuestsOnIsland.ToArray())
                {
                    if ((botWithQuestsOnIsland == null) || botWithQuestsOnIsland.IsDead)
                    {
                        botsWithQuestsOnIsland.Remove(botWithQuestsOnIsland);
                        continue;
                    }

                    formAlliancesWithZryachiyAndFollowers(botWithQuestsOnIsland);
                }
            }

            if (playersOnIsland.Contains(player) != isOnIsland)
            {
                toggleHostility(player);
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
                formAlliance(bot, zryachiyOrFollower);
                formAlliance(zryachiyOrFollower, bot);
            }
        }

        private void toggleHostility(Player player)
        {
            if (playersOnIsland.Contains(player))
            {
                LoggingController.LogInfo(player.GetText() + " has left Lightkeeper Island");
                playersOnIsland.Remove(player);

                revertAlliances(player);
                foreach (Player otherPlayer in playersOnIsland)
                {
                    if (player.Profile.Id == otherPlayer.Profile.Id)
                    {
                        continue;
                    }

                    revertAlliances(otherPlayer, player);
                }
            }
            else
            {
                LoggingController.LogInfo(player.GetText() + " has entered Lightkeeper Island");
                playersOnIsland.Add(player);

                setTemporaryAlliances(player);
            }
        }

        private void setTemporaryAlliances(Player player)
        {
            setOriginalAllies(player);
            setOriginalEnemies(player);

            foreach (Player otherPlayer in playersOnIsland)
            {
                if (player.Profile.Id == otherPlayer.Profile.Id)
                {
                    continue;
                }

                formAlliance(player, otherPlayer);
                formAlliance(otherPlayer, player);
            }
        }

        private void formAlliance(IPlayer iplayer, IPlayer otherIPlayer)
        {
            if ((iplayer == null) || (otherIPlayer == null))
            {
                return;
            }

            BotsGroup playerGroup = iplayer.GetBotOwner()?.BotsGroup;
            if (playerGroup == null)
            {
                return;
            }

            IPlayer[] enemyMatches = playerGroup.Enemies
                    .Where(e => e.Key.Profile.Id == otherIPlayer.Profile.Id)
                    .Select(e => e.Key)
                    .ToArray();

            foreach (IPlayer remainingEnemy in enemyMatches)
            {
                LoggingController.LogInfo(iplayer.GetText() + "'s group has paused their hostility with " + remainingEnemy.GetText());
                playerGroup.RemoveEnemy(remainingEnemy);
            }

            Player otherPlayer = otherIPlayer.GetPlayer();
            if (!playerGroup.Allies.Contains(otherPlayer))
            {
                LoggingController.LogInfo(iplayer.GetText() + "'s group is temporarily allied with " + otherPlayer.GetText());
                playerGroup.AddAlly(otherPlayer);
            }
        }

        private void revertAlliances(Player player, Player otherPlayer = null)
        {
            BotsGroup playerGroup = player.GetBotOwner()?.BotsGroup;
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

                    BotOwner allyOwner = ally.GetBotOwner();
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

        private void setOriginalAllies(Player player)
        {
            BotsGroup playerGroup = player.GetBotOwner()?.BotsGroup;
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

        private void setOriginalEnemies(Player player)
        {
            BotsGroup playerGroup = player.GetBotOwner()?.BotsGroup;
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
    }
}
