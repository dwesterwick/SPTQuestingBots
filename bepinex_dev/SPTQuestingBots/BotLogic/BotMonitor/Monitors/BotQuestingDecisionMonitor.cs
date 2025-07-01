using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.BotMonitor
{
    public enum BotQuestingDecision
    {
        None,
        Inactive,
        Sleep,
        WaitForQuestData,
        Fight,
        HelpBoss,
        HelpGroup,
        FollowBoss,
        Regroup,
        Investigtate,
        Hunt,
        StopToHeal,
        CheckForLoot,
        UseStationaryWeapon,
        Stuck,
        Quest,
        WaitForAssignment,
    }

    public class BotQuestingDecisionMonitor : AbstractBotMonitor
    {
        public BotQuestingDecision CurrentDecision { get; private set; } = BotQuestingDecision.None;
        public bool HasAQuestingBoss { get; private set; } = false;

        private Components.BotQuestBuilder botQuestBuilder;

        private bool allowedToTakeABreak() => ObjectiveManager.IsAllowedToTakeABreak();
        private bool allowedToInvestigate() => ObjectiveManager.IsAllowedToInvestigate();

        public BotQuestingDecisionMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public bool IsAllowedToQuest()
        {
            if
            (
                CurrentDecision == BotQuestingDecision.None
                || CurrentDecision == BotQuestingDecision.Inactive
                || CurrentDecision == BotQuestingDecision.WaitForQuestData
            )
            {
                return false;
            }

            return true;
        }

        public override void Start()
        {
            botQuestBuilder = Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>();
        }

        public void ForceDecision(BotQuestingDecision decision)
        {
            CurrentDecision = decision;
        }

        public override void Update()
        {
            HasAQuestingBoss = BotMonitor.GetMonitor<BotQuestingMonitor>().HasAQuestingBoss;
            CurrentDecision = getDecision();
        }

        private BotQuestingDecision getDecision()
        {
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                return BotQuestingDecision.None;
            }

            if (!BotOwner.IsAlive())
            {
                return BotQuestingDecision.Inactive;
            }

            if (HasAQuestingBoss)
            {
                return getFollowerDecision();
            }

            return getSoloDecision();
        }

        private BotQuestingDecision getFollowerDecision()
        {
            Controllers.BotJobAssignmentFactory.InactivateAllJobAssignmentsForBot(BotOwner.Profile.Id);

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat)
            {
                return BotQuestingDecision.Fight;
            }

            if (BotMonitor.GetMonitor<BotHearingMonitor>().IsSuspicious)
            {
                return BotQuestingDecision.Investigtate;
            }

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsSAINLayerActive())
            {
                return BotQuestingDecision.Hunt;
            }

            if (BotMonitor.GetMonitor<BotHealthMonitor>().NeedsToHeal)
            {
                return BotQuestingDecision.StopToHeal;
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().StuckTooManyTimes)
            {
                return BotQuestingDecision.Stuck;
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().DoesBossNeedHelp && isFollowerTooFarFromBossForCombat())
            {
                return BotQuestingDecision.HelpBoss;
            }

            if (!isFollowerTooFarFromBossForQuesting())
            {
                return BotQuestingDecision.None;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return BotQuestingDecision.HelpGroup;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return BotQuestingDecision.HelpGroup;
            }

            if (BotMonitor.GetMonitor<BotLootingMonitor>().BossWillAllowLooting)
            {
                setLootingHiveMindState(true);
                return BotQuestingDecision.CheckForLoot;

            }
            setLootingHiveMindState(false);

            return BotQuestingDecision.FollowBoss;
        }

        private BotQuestingDecision getSoloDecision()
        {
            if (!ObjectiveManager.IsQuestingAllowed)
            {
                return BotQuestingDecision.None;
            }

            if (!botQuestBuilder.HaveQuestsBeenBuilt)
            {
                return BotQuestingDecision.WaitForQuestData;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotMountedGunMonitor>().WantsToUseStationaryWeapon)
            {
                return BotQuestingDecision.UseStationaryWeapon;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotExtractMonitor>().IsTryingToExtract)
            {
                ObjectiveManager.StopQuesting();

                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " wants to extract and will no longer quest.");
                return BotQuestingDecision.None;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat)
            {
                return BotQuestingDecision.Fight;
            }

            if (allowedToInvestigate() && BotMonitor.GetMonitor<BotHearingMonitor>().IsSuspicious)
            {
                return BotQuestingDecision.Investigtate;
            }

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsSAINLayerActive())
            {
                return BotQuestingDecision.Hunt;
            }

            if (BotMonitor.GetMonitor<BotHealthMonitor>().NeedsToHeal)
            {
                return BotQuestingDecision.StopToHeal;
            }

            if (allowedToTakeABreak() && BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return BotQuestingDecision.HelpGroup;
            }

            if (allowedToInvestigate() && BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return BotQuestingDecision.HelpGroup;
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().StuckTooManyTimes)
            {
                return BotQuestingDecision.Stuck;
            }

            // Check if the bot wants to loot
            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotLootingMonitor>().ShouldCheckForLoot())
            {
                setLootingHiveMindState(true);

                return BotQuestingDecision.CheckForLoot;
            }
            setLootingHiveMindState(false);

            // Check if the bot has wandered too far from its followers.
            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotQuestingMonitor>().NeedToRegroupWithFollowers)
            {
                return BotQuestingDecision.Regroup;
            }

            // Check if the bot needs to complete its assignment
            if (!ObjectiveManager.IsJobAssignmentActive)
            {
                return BotQuestingDecision.WaitForAssignment;
            }

            return BotQuestingDecision.Quest;
        }

        private void setLootingHiveMindState(bool value) => BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.WantsToLoot, BotOwner, value);

        private bool isFollowerTooFarFromBossForQuesting() => BotMonitor.GetMonitor<BotQuestingMonitor>().DistanceToBoss > getFollowerTargetDistanceQuesting();
        private double getFollowerTargetDistanceQuesting()
        {
            MinMaxConfig targetFollowerRangeQuesting = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeQuesting;

            if (CurrentDecision == BotQuestingDecision.FollowBoss)
            {
                return targetFollowerRangeQuesting.Min;
            }

            return targetFollowerRangeQuesting.Max;
        }

        private bool isFollowerTooFarFromBossForCombat() => BotMonitor.GetMonitor<BotQuestingMonitor>().DistanceToBoss > getFollowerTargetDistanceCombat();
        private double getFollowerTargetDistanceCombat()
        {
            MinMaxConfig targetFollowerRangeQuesting = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat;

            if (CurrentDecision == BotQuestingDecision.HelpBoss)
            {
                return targetFollowerRangeQuesting.Min;
            }

            return targetFollowerRangeQuesting.Max;
        }
    }
}
