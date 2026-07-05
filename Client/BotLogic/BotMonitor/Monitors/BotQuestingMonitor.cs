using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.ExternalMods.ModInfo;
using QuestingBots.BotLogic.Follow;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.BotLogic.Objective;
using QuestingBots.Controllers;
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
        public bool IsRegrouping { get; private set; } = false;
        public bool ShouldWaitForFollowers { get; private set; } = false;
        public bool FollowersNeedToTeleport { get; private set; } = false;

        private Stopwatch followersTooFarTimer = new Stopwatch();

        public float DistanceToBoss => BotHiveMindMonitor.GetDistanceToBoss(BotOwner);
        public bool NeedToRegroupWithFollowers => followersTooFarTimer.ElapsedMilliseconds > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.MaxWaitTime * 1000;
        public bool StuckTooManyTimes => ObjectiveManager.StuckCount >= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxCount;

        public BotQuestingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void UpdateIfQuesting()
        {
            HasABoss = BotHiveMindMonitor.HasBoss(BotOwner);
            HasAQuestingBoss = HasABoss && BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.CanQuest, BotOwner);
            DoesBossNeedHelp = HasABoss && doesBossNeedHelp();

            IsQuesting = isQuesting();
            IsFollowing= isFollowing();
            IsRegrouping = isRegrouping();
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

            if (ObjectiveManager.IsQuestingAllowed && BotMonitor.GetMonitor<BotQuestingMonitor>().StuckTooManyTimes)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Bot " + BotOwner.GetText() + " was stuck " + ObjectiveManager.StuckCount + " times and likely is unable to quest.");
                ObjectiveManager.StopQuesting();
                BotOwner.Mover.Stop();
                BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);
            }
        }

        private bool isQuesting() => BotOwner.IsLayerActive(nameof(BotObjectiveLayer));
        private bool isFollowing() => BotOwner.IsLayerActive(nameof(BotFollowerLayer));
        private bool isRegrouping() => BotOwner.IsLogicActive(nameof(BossRegroupAction));

        private bool shouldWaitForFollowers()
        {
            // Check if the bot has any followers
            IEnumerable<BotOwner> activeFollowers = HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner)
                .Where(f => (f != null) && !f.IsDead)
                .Where(f => f.GetObjectiveManager()?.PrioritizeQuestingOverFollowing != true);

            if (!activeFollowers.Any())
            {
                return false;
            }

            // Check if the bot is too far from any of its followers
            IEnumerable<float> followerDistances = activeFollowers
                .Select(f => Vector3.Distance(BotOwner.Position, f.Position));

            if
            (
                followerDistances.Any(d => d > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Furthest)
                || followerDistances.All(d => d > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Nearest)
            )
            {
                return true;
            }

            return false;
        }

        private bool doesBossNeedHelp()
        {
            if (SAINModInfo.IsSAINLayer(BotHiveMindMonitor.GetActiveBrainLayerOfBoss(BotOwner) ?? "") == true)
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
            IReadOnlyCollection<BotOwner> followers = HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner);
            foreach (BotOwner follower in followers)
            {
                Components.BotObjectiveManager? followerObjectiveManager = follower.GetObjectiveManager();
                if (followerObjectiveManager == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for follower " + follower.GetText());
                    continue;
                }

                if (followerObjectiveManager.HasTeleportingAssignment)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
