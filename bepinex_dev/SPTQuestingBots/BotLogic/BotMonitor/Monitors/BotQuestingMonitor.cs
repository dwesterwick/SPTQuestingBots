using EFT;
using SPTQuestingBots.BotLogic.Follow;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotQuestingMonitor : AbstractBotMonitor
    {
        public bool IsQuesting { get; private set; } = false;
        public bool IsFollowing { get; private set; } = false;
        public bool IsRegrouping { get; private set; } = false;
        public bool ShouldWaitForFollowers { get; private set; } = false;

        public BotQuestingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Update()
        {
            IsQuesting = isQuesting();
            IsFollowing= isFollowing();
            IsRegrouping = isRegrouping();
            ShouldWaitForFollowers = shouldWaitForFollowers();
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
    }
}
