using EFT;
using SPTQuestingBots.BotLogic.ExternalMods.ModInfo;
using SPTQuestingBots.BotLogic.Follow;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
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

        private Stopwatch followersTooFarTimer = new Stopwatch();

        public float DistanceToBoss => BotHiveMindMonitor.GetDistanceToBoss(BotOwner);
        public bool NeedToRegroupWithFollowers => followersTooFarTimer.ElapsedMilliseconds > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.MaxWaitTime * 1000;
        public bool StuckTooManyTimes => ObjectiveManager.StuckCount >= ConfigController.Config.Questing.StuckBotDetection.MaxCount;

        public BotQuestingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Update()
        {
            HasABoss = BotHiveMindMonitor.HasBoss(BotOwner);
            HasAQuestingBoss = HasABoss && BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.CanQuest, BotOwner);
            DoesBossNeedHelp = HasABoss && doesBossNeedHelp();

            IsQuesting = isQuesting();
            IsFollowing= isFollowing();
            IsRegrouping = isRegrouping();
            ShouldWaitForFollowers = shouldWaitForFollowers();

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
                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " was stuck " + ObjectiveManager.StuckCount + " times and likely is unable to quest.");
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
            IReadOnlyCollection<BotOwner> followers = HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner);
            if (followers.Count == 0)
            {
                return false;
            }

            // Check if the bot is too far from any of its followers
            IEnumerable<float> followerDistances = followers
                .Where(f => (f != null) && !f.IsDead)
                .Select(f => Vector3.Distance(BotOwner.Position, f.Position));

            if
            (
                followerDistances.Any(d => d > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.Furthest)
                || followerDistances.All(d => d > ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.Nearest)
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
    }
}
