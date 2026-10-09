using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.Follow;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.BotLogic.Objective;
using QuestingBots.Configuration;
using QuestingBots.Controllers;
using QuestingBots.ExternalMods.LoadedModInfo;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotQuestingMonitor : AbstractBotMonitor
    {
        public bool HasABoss { get; private set; } = false;
        public bool HasAQuestingBoss { get; private set; } = false;
        public bool DoesBossNeedHelp { get; private set; } = false;

        public bool IsQuesting { get; private set; } = false;
        public bool IsFollowing { get; private set; } = false;
        public bool IsInvestigating { get; private set; } = false;
        public bool IsRecovering { get; private set; } = false;
        public MinMaxConfig? FollowerDistanceRangeOverall { get; private set; } = null;
        public MinMaxConfig? FollowerDistanceRangeFollowing { get; private set; } = null;
        public bool ShouldWaitForFollowers { get; private set; } = false;
        public bool FollowersNeedToTeleport { get; private set; } = false;
        
        private Stopwatch followersTooFarTimer = new Stopwatch();

        public bool NeedToRegroupWithFollowers => followersTooFarTimer.ElapsedMilliseconds > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.MaxWaitTime * 1000;
        public bool StuckTooManyTimes => (ObjectiveManager != null) && (ObjectiveManager.StuckCount >= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxCount);

        public BotQuestingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void UpdateIfQuesting()
        {
            if ((ObjectiveManager == null) || (BotMonitor == null))
            {
                return;
            }

            HasABoss = BotHiveMindMonitor.HasGroupLeader(BotOwner);
            HasAQuestingBoss = HasABoss && BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.CanQuest, BotOwner);
            DoesBossNeedHelp = HasABoss && doesBossNeedHelp();

            IsQuesting = BotOwner.IsQuesting();
            IsFollowing = BotOwner.IsFollowing();
            IsInvestigating = BotOwner.IsInvestigating();
            IsRecovering = BotOwner.IsRecovering();

            updateFollowerDistanceRange();
            ShouldWaitForFollowers = shouldWaitForFollowers();
            FollowersNeedToTeleport = followersNeedToTeleport();

            if (ShouldWaitForFollowers)
            {
                followersTooFarTimer.Start();
            }
            else
            {
                followersTooFarTimer.Reset();
            }

            if (ObjectiveManager.IsQuestingAllowed && StuckTooManyTimes)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Bot " + BotOwner.GetText() + " was stuck " + ObjectiveManager.StuckCount + " times and likely is unable to quest.");
                ObjectiveManager.StopQuesting();
                BotOwner.Mover.Stop();
                BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);
            }
        }

        public float GetDistanceToBoss() => BotHiveMindMonitor.GetDistanceToGroupLeader(BotOwner);

        private void updateFollowerDistanceRange()
        {
            IEnumerable<BotOwner> totalFollowers = HiveMind.BotHiveMindMonitor.GetGroupFollowers(BotOwner)
                .WhereNonAlloc(f => (f != null) && !f.IsDead)
                .WhereNonAlloc(f => f.GetObjectiveManager()?.PrioritizeQuestingOverFollowing != true);

            if (canUpdateDistanceRange(totalFollowers, out double nearestDistance, out double furthestDistance))
            {
                if (FollowerDistanceRangeOverall == null)
                {
                    FollowerDistanceRangeOverall = new MinMaxConfig(nearestDistance, furthestDistance);
                }
                else
                {
                    FollowerDistanceRangeOverall.Min = nearestDistance;
                    FollowerDistanceRangeOverall.Max = furthestDistance;
                }
            }
            else
            {
                FollowerDistanceRangeOverall = null;
            }

            IEnumerable<BotOwner> followingFollowers = totalFollowers
                .Where(follower => follower.IsFollowing());

            if (canUpdateDistanceRange(followingFollowers, out nearestDistance, out furthestDistance))
            {
                if (FollowerDistanceRangeFollowing == null)
                {
                    FollowerDistanceRangeFollowing = new MinMaxConfig(nearestDistance, furthestDistance);
                }
                else
                {
                    FollowerDistanceRangeFollowing.Min = nearestDistance;
                    FollowerDistanceRangeFollowing.Max = furthestDistance;
                }
            }
            else
            {
                FollowerDistanceRangeFollowing = null;
            }
        }

        private bool canUpdateDistanceRange(IEnumerable<BotOwner> bots, out double nearestDistance, out double furthestDistance)
        {
            nearestDistance = float.MaxValue;
            furthestDistance = 0;

            foreach (BotOwner bot in bots)
            {
                float distance = Vector3.Distance(BotOwner.Position, bot.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }

                if (distance > furthestDistance)
                {
                    furthestDistance = distance;
                }
            }

            return nearestDistance != float.MaxValue;
        }

        private bool shouldWaitForFollowers()
        {
            if (FollowerDistanceRangeOverall != null)
            {
                if (FollowerDistanceRangeOverall.Max > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Furthest)
                {
                    return true;
                }

                if (ShouldWaitForFollowers)
                {
                    if (FollowerDistanceRangeOverall.Max > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeQuesting.Max)
                    {
                        return true;
                    }
                }
            }

            if (FollowerDistanceRangeFollowing != null)
            {
                if (FollowerDistanceRangeFollowing.Min > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Nearest)
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool doesBossNeedHelp()
        {
            if (SAINModInfo.IsSAINLayer(BotHiveMindMonitor.GetActiveBrainLayerOfGroupLeader(BotOwner) ?? "") == true)
            {
                return true;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return true;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return true;
            }

            return false;
        }

        private bool followersNeedToTeleport()
        {
            IReadOnlyCollection<BotOwner> followers = HiveMind.BotHiveMindMonitor.GetGroupFollowers(BotOwner);
            foreach (BotOwner follower in followers)
            {
                // This shouldn't happen, but it does...
                if (follower.Id == BotOwner.Id)
                {
                    continue;
                }

                Components.BotObjectiveManager? followerObjectiveManager = follower.GetObjectiveManager();
                if (followerObjectiveManager == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for follower " + follower.GetText());
                    continue;
                }

                if (followerObjectiveManager.HasTeleportingAssignment)
                {
                    if (!FollowersNeedToTeleport)
                    {
                        Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " will wait for " + follower.GetText() + " to teleport for its quest");
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
