using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SPT.Custom.CustomAI;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Components
{
    public class LightkeeperIslandMonitor : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        public static PhysicsTriggerHandler LightkeeperTraderZoneColliderHandler { get; set; } = null;

        private LocationData locationData;
        private List<Player> playersOnIsland = new List<Player>();
        private List<BotOwner> botsWithQuestsOnIsland = new List<BotOwner>();
        private Dictionary<Player, IPlayer[]> originalAllies = new Dictionary<Player, IPlayer[]>();
        private Dictionary<Player, IPlayer[]> originalEnemies = new Dictionary<Player, IPlayer[]>();

        public IReadOnlyList<Player> PlayersOnLightkeeperIsland => playersOnIsland.AsReadOnly();
        public IReadOnlyList<BotOwner> BotsWithQuestsOnLightkeeperIsland => botsWithQuestsOnIsland.AsReadOnly();

        protected void Awake()
        {
            locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();

            if (LightkeeperTraderZoneColliderHandler == null)
            {
                throw new InvalidOperationException("LightkeeperTraderZoneColliderHandler was never initialized by LighthouseTraderZoneAwakePatch");
            }

            LighthouseTraderZone.OnPlayerAllowStatusChanged += playerAllowStatusChanged;

            if (ConfigController.Config.Debug.ShowZoneOutlines && Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out PathRenderer pathRender))
            {
                Vector3[] colliderBounds = DebugHelpers.GetBoundingBoxPoints(LightkeeperTraderZoneColliderHandler.trigger.bounds);
                Models.Pathing.PathVisualizationData zoneBoundingBox = new Models.Pathing.PathVisualizationData("LighthouseTraderZone", colliderBounds, Color.green);

                pathRender.AddOrUpdatePath(zoneBoundingBox);
            }
        }

        protected void OnDestroy()
        {
            LighthouseTraderZone.OnPlayerAllowStatusChanged -= playerAllowStatusChanged;
        }

        protected void Update()
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

        private static void playerAllowStatusChanged(string profileID, bool status)
        {
            if (status == true)
            {
                return;
            }

            Player player = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(profileID);
            if (player.IsAI)
            {
                return;
            }

            if (!player.HasAGreenOrYellowDSP())
            {
                return;
            }

            IEnumerable<BotOwner> zryachiyAndFollowers = BotGroupHelpers.FindZryachiyAndFollowers();
            if (!zryachiyAndFollowers.Any())
            {
                return;
            }

            zryachiyAndFollowers.First().BotsGroup.AddEnemy(player, EBotEnemyCause.zryachiyLogic);
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

        public bool IsPointOnLightkeeperIsland(Vector3 position)
        {
            if (LightkeeperTraderZoneColliderHandler != null)
            {
                return LightkeeperTraderZoneColliderHandler.trigger.bounds.Contains(position);
            }

            LoggingController.LogWarning("LightkeeperTraderZoneColliderHandler is null. Using alternative check for position being on the island.");
            return position.z > 325 && position.x > 183;
        }

        public bool IsPlayerOnLightkeeperIsland(Player player)
        {
            bool isOnIsland = locationData.IsPointOnLightkeeperIsland(player.Position);

            if (player.IsZryachiyOrFollower())
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
            BotObjectiveManager botObjectiveManager = bot.GetObjectiveManager();
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
                foreach (BotOwner member in bot.BotsGroup.GetAllMembers())
                {
                    botsWithQuestsOnIsland.Add(member);
                    formAlliancesWithZryachiyAndFollowers(member);
                }
            }

            return isQuestOnIsland;
        }

        private void formAlliancesWithZryachiyAndFollowers(BotOwner bot)
        {
            foreach (BotOwner zryachiyOrFollower in BotGroupHelpers.FindZryachiyAndFollowers())
            {
                bot.FormAlliance(zryachiyOrFollower);
                zryachiyOrFollower.FormAlliance(bot);
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

                if (!canSetAlliance(player, otherPlayer))
                {
                    LoggingController.LogWarning("Cannot force an alliance between a human player and Zryachiy or his followers");
                    continue;
                }

                player.FormAlliance(otherPlayer);
                otherPlayer.FormAlliance(player);
            }
        }

        private bool canSetAlliance(IPlayer player, IPlayer otherPlayer)
        {
            bool atLeastOneHuman = false;
            if (!player.IsAI || !otherPlayer.IsAI)
            {
                atLeastOneHuman = true;
            }

            bool atLeastOneZryachiyOrFollower = false;
            if (player.IsZryachiyOrFollower() || otherPlayer.IsZryachiyOrFollower())
            {
                atLeastOneZryachiyOrFollower = true;
            }

            return !(atLeastOneHuman && atLeastOneZryachiyOrFollower);
        }

        private void revertAlliances(Player player, Player otherPlayer = null)
        {
            BotsGroup playerGroup = player?.GetBotOwner()?.BotsGroup;
            if (playerGroup == null)
            {
                return;
            }

            foreach (IPlayer ally in playerGroup.Allies.ToArray())
            {
                if ((otherPlayer != null) && (ally?.Profile?.Id != otherPlayer.Profile.Id))
                {
                    continue;
                }

                if (!originalAllies[player].Contains(ally))
                {
                    LoggingController.LogDebug(player.GetText() + "'s group is no longer allied with " + ally.GetText());

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
                if ((otherPlayer != null) && (enemy?.Profile?.Id != otherPlayer.Profile.Id))
                {
                    continue;
                }

                if (!playerGroup.Enemies.ContainsKey(enemy))
                {
                    LoggingController.LogDebug(player.GetText() + "'s group has restored their hostility with " + enemy.GetText());
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
