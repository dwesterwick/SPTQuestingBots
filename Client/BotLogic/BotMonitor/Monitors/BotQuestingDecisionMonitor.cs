using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Configuration;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.BotMonitor
{
    public enum EBotQuestingDecision
    {
        None,
        Inactive,
        Sleep,
        WaitForQuestData,
        Fight,
        HelpBoss,
        WaitForGroup,
        FollowBoss,
        Regroup,
        Investigtate,
        Hunt,
        StopToHeal,
        CheckForLoot,
        UseStationaryWeapon,
        GetLost,
        Quest,
        WaitForAssignment,
    }

    public class BotQuestingDecisionMonitor : AbstractBotMonitor
    {
        public EBotQuestingDecision CurrentDecision { get; private set; } = EBotQuestingDecision.None;
        public bool HasAQuestingBoss { get; private set; } = false;

        private Components.BotQuestBuilder botQuestBuilder = null!;

        public bool MustQuestBeforeFollowing => (ObjectiveManager != null) && (ObjectiveManager.PrioritizeQuestingOverFollowing || ObjectiveManager.HasTeleportingAssignment);

        private bool allowedToTakeABreak() => (ObjectiveManager != null) && ObjectiveManager.IsAllowedToTakeABreak();
        private bool allowedToInvestigate() => (ObjectiveManager != null) && ObjectiveManager.IsAllowedToInvestigate();

        public BotQuestingDecisionMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public bool IsAllowedToQuest()
        {
            if
            (
                CurrentDecision == EBotQuestingDecision.None
                || CurrentDecision == EBotQuestingDecision.Inactive
                || CurrentDecision == EBotQuestingDecision.WaitForQuestData
            )
            {
                return false;
            }

            return true;
        }

        public bool ShouldFollowBoss()
        {
            if (!HasAQuestingBoss)
            {
                return false;
            }

            if (ObjectiveManager == null)
            {
                return false;
            }

            if (ObjectiveManager.PrioritizeQuestingOverFollowing || ObjectiveManager.HasTeleportingAssignment)
            {
                return false;
            }

            return true; 
        }

        public override void Start()
        {
            botQuestBuilder = Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>();
        }

        public void ForceDecision(EBotQuestingDecision decision)
        {
            CurrentDecision = decision;
        }

        public override void UpdateIfQuesting()
        {
            if (BotMonitor == null)
            {
                return;
            }

            HasAQuestingBoss = BotMonitor.GetMonitor<BotQuestingMonitor>().HasAQuestingBoss;
            CurrentDecision = getDecision();
        }

        private EBotQuestingDecision getDecision()
        {
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                return EBotQuestingDecision.None;
            }

            if (!BotOwner.IsAlive())
            {
                return EBotQuestingDecision.Inactive;
            }

            if (HasAQuestingBoss && !MustQuestBeforeFollowing)
            {
                return getFollowerDecision();
            }

            return getSoloDecision();
        }

        private EBotQuestingDecision getFollowerDecision()
        {
            if (BotMonitor == null)
            {
                return EBotQuestingDecision.None;
            }

            Controllers.BotJobAssignmentController.InactivateAllJobAssignmentsForBot(BotOwner.Profile.Id);

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat)
            {
                return EBotQuestingDecision.Fight;
            }

            if (BotMonitor.GetMonitor<BotHearingMonitor>().IsSuspicious)
            {
                return EBotQuestingDecision.Investigtate;
            }

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsSAINLayerActive())
            {
                return EBotQuestingDecision.Hunt;
            }

            if (BotMonitor.GetMonitor<BotHealthMonitor>().NeedsToHeal)
            {
                return EBotQuestingDecision.StopToHeal;
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().StuckTooManyTimes)
            {
                return EBotQuestingDecision.GetLost;
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().DoesBossNeedHelp && isFollowerTooFarFromBossForCombat())
            {
                return EBotQuestingDecision.HelpBoss;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return EBotQuestingDecision.WaitForGroup;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return EBotQuestingDecision.WaitForGroup;
            }

            if (BotMonitor.GetMonitor<BotLootingMonitor>().IsForcedToSearchForLoot && BotMonitor.GetMonitor<BotLootingMonitor>().BossWillAllowLootingByDistance)
            {
                setLootingHiveMindState(true);
                return EBotQuestingDecision.CheckForLoot;
            }

            if (BotMonitor.GetMonitor<BotLootingMonitor>().BossWillAllowLooting)
            {
                setLootingHiveMindState(true);
                return EBotQuestingDecision.CheckForLoot;
            }

            setLootingHiveMindState(false);

            if (!isFollowerTooFarFromBossForQuesting())
            {
                return EBotQuestingDecision.None;
            }

            return EBotQuestingDecision.FollowBoss;
        }

        private EBotQuestingDecision getSoloDecision()
        {
            if ((ObjectiveManager == null) || (BotMonitor == null))
            {
                return EBotQuestingDecision.None;
            }

            if (!ObjectiveManager.IsQuestingAllowed)
            {
                return EBotQuestingDecision.None;
            }

            if (!botQuestBuilder.HaveQuestsBeenBuilt)
            {
                return EBotQuestingDecision.WaitForQuestData;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotMountedGunMonitor>().WantsToUseStationaryWeapon)
            {
                return EBotQuestingDecision.UseStationaryWeapon;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotExtractMonitor>().IsTryingToExtract)
            {
                ObjectiveManager.StopQuesting();

                Singleton<LoggingUtil>.Instance.LogWarning("Bot " + BotOwner.GetText() + " wants to extract and will no longer quest.");
                return EBotQuestingDecision.None;
            }

            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat)
            {
                return EBotQuestingDecision.Fight;
            }

            if (allowedToInvestigate() && BotMonitor.GetMonitor<BotHearingMonitor>().IsSuspicious)
            {
                return EBotQuestingDecision.Investigtate;
            }

            if (BotMonitor.GetMonitor<BotCombatMonitor>().IsSAINLayerActive())
            {
                return EBotQuestingDecision.Hunt;
            }

            if (BotMonitor.GetMonitor<BotHealthMonitor>().NeedsToHeal)
            {
                return EBotQuestingDecision.StopToHeal;
            }

            if (!MustQuestBeforeFollowing)
            {
                if (allowedToTakeABreak() && BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
                {
                    return EBotQuestingDecision.WaitForGroup;
                }

                if (allowedToInvestigate() && BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
                {
                    return EBotQuestingDecision.WaitForGroup;
                }
            }

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().StuckTooManyTimes)
            {
                return EBotQuestingDecision.GetLost;
            }

            if (BotMonitor.GetMonitor<BotLootingMonitor>().IsForcedToSearchForLoot)
            {
                setLootingHiveMindState(true);
                return EBotQuestingDecision.CheckForLoot;
            }

            // Check if the bot wants to loot
            if (allowedToTakeABreak() && BotMonitor.GetMonitor<BotLootingMonitor>().ShouldCheckForLoot())
            {
                setLootingHiveMindState(true);
                return EBotQuestingDecision.CheckForLoot;
            }

            setLootingHiveMindState(false);

            if (BotMonitor.GetMonitor<BotQuestingMonitor>().FollowersNeedToTeleport)
            {
                return EBotQuestingDecision.WaitForGroup;
            }

            // Check if the bot has wandered too far from its followers.
            if (allowedToTakeABreak() && !MustQuestBeforeFollowing && BotMonitor.GetMonitor<BotQuestingMonitor>().NeedToRegroupWithFollowers)
            {
                return EBotQuestingDecision.Regroup;
            }

            // Check if the bot needs to complete its assignment
            if (!ObjectiveManager.IsJobAssignmentActive)
            {
                return EBotQuestingDecision.WaitForAssignment;
            }

            return EBotQuestingDecision.Quest;
        }

        private void setLootingHiveMindState(bool value) => BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.WantsToLoot, BotOwner, value);

        private bool isFollowerTooFarFromBossForQuesting()
        {
            if (BotMonitor == null)
            {
                return false;
            }

            return BotMonitor.GetMonitor<BotQuestingMonitor>().GetDistanceToBoss() > getFollowerTargetDistanceQuesting();
        }

        private double getFollowerTargetDistanceQuesting()
        {
            MinMaxConfig targetFollowerRangeQuesting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeQuesting;

            if (CurrentDecision == EBotQuestingDecision.FollowBoss)
            {
                return targetFollowerRangeQuesting.Min;
            }

            return targetFollowerRangeQuesting.Max;
        }

        private bool isFollowerTooFarFromBossForCombat()
        {
            if (BotMonitor == null)
            {
                return false;
            }

            return BotMonitor.GetMonitor<BotQuestingMonitor>().GetDistanceToBoss() > getFollowerTargetDistanceCombat();
        }

        private double getFollowerTargetDistanceCombat()
        {
            MinMaxConfig targetFollowerRangeQuesting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat;

            if (CurrentDecision == EBotQuestingDecision.HelpBoss)
            {
                return targetFollowerRangeQuesting.Min;
            }

            return targetFollowerRangeQuesting.Max;
        }
    }
}
