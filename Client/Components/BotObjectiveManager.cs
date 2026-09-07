using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Pathing;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Components
{
    public class BotObjectiveManager : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        public bool HasInitialQuestBeenSelected { get; private set; } = false;
        public bool IsQuestingAllowed { get; private set; } = false;
        public BotJobAssignment? CurrentAssignment { get; private set; } = null;
        public int StuckCount { get; set; } = 0;
        public float PauseRequest { get; set; } = 0;
        public Models.BotSprintingController BotSprintingController { get; private set; } = null!;
        public BotPathData BotPath { get; private set; } = null!;
        public BotLogic.BotMonitor.BotMonitorController BotMonitor { get; private set; } = null!;
        public BotIdentityData IdentityData { get; private set; } = null!;
        public BotQuestSelector QuestSelector { get; private set; } = null!;
        public EFT.Interactive.Door DoorToOpen { get; set; } = null!;
        public Vector3? LastCorner { get; set; } = null;

        private BotOwner botOwner = null!;
        private bool isInitializing = false;
        private bool isInitialized = false;
        private BotJobAssignment? lastAssignment = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();

        public Vector3? Position => CurrentAssignment?.Position;
        public Vector3? LookToPosition => CurrentAssignment?.LookToPosition;
        public Vector3? TargetPosition => CurrentAssignment?.TargetPosition;
        public bool IsJobAssignmentActive => CurrentAssignment?.IsActive == true;
        public bool HasTeleportingAssignment => CurrentAssignment?.MustTeleport == true;
        public bool HasCompletePath => CurrentAssignment?.HasCompletePath ?? false;
        public string DoorIDToUnlockForObjective => CurrentAssignment?.QuestObjectiveAssignment?.DoorIDToUnlock ?? "";
        public Vector3? InteractionPositionForDoorToUnlockForObjective => CurrentAssignment?.QuestObjectiveAssignment?.InteractionPositionToUnlockDoor?.ToUnityVector3();
        public bool MustUnlockDoor => CurrentAssignment?.DoorToUnlock != null;
        public QuestAction CurrentQuestAction => CurrentAssignment?.QuestObjectiveStepAssignment?.ActionType ?? QuestAction.Undefined;
        public double MinElapsedActionTime => CurrentAssignment?.MinElapsedTime ?? 0;
        public float ChanceOfHavingKey => CurrentAssignment?.QuestObjectiveStepAssignment?.ChanceOfHavingKey ?? 0;
        public float? MaxDistanceForCurrentStep => CurrentAssignment?.QuestObjectiveStepAssignment?.MaxDistance;
        public bool IgnoreHearing => CurrentAssignment?.IgnoreHearing ?? false;
        public bool ForceUnlock => CurrentAssignment?.ForceUnlock ?? false;
        public bool PrioritizeQuestingOverFollowing => CurrentAssignment?.PrioritizeOverFollowing ?? false;
        public double? WaitTimeAfterCompleting => CurrentAssignment?.QuestObjectiveStepAssignment?.WaitTimeAfterCompleting;

        public double TimeSpentAtObjective => timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0;
        public float DistanceToObjective => Position.HasValue ? Vector3.Distance(Position.Value, botOwner.Position) : float.NaN;
        public float DistanceFromLastObjective => (lastAssignment?.Position != null) ? Vector3.Distance(lastAssignment.Position.Value, botOwner.Position) : float.MaxValue;

        public bool IsCloseToObjective(float distance) => DistanceToObjective <= distance;
        public bool IsCloseToObjective() => IsCloseToObjective(Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotSearchDistances.OjectiveReachedIdeal);

        public void StartJobAssigment() => CurrentAssignment?.Start();
        
        public double? TimeSinceJobAssigmentStarted() => CurrentAssignment?.TimeSinceStarted();

        public override string ToString()
        {
            if (CurrentAssignment?.QuestAssignment != null)
            {
                return CurrentAssignment.ToString();
            }

            return "Position " + (Position?.ToString() ?? "???");
        }

        public void Init(BotOwner _botOwner)
        {
            if (isInitialized || isInitializing)
            {
                return;
            }

            isInitializing = true;

            base.UpdateInterval = 200;
            botOwner = _botOwner;

            // Override the EFT distance that makes bots "avoid danger" when the BTR is near
            float newAvoidBtrRadiusSqr = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BTRRunDistance * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BTRRunDistance;
            botOwner.Settings.FileSettings.Mind.AVOID_BTR_RADIUS_SQR = newAvoidBtrRadiusSqr;

            StartCoroutine(createComponents());
        }

        private IEnumerator createComponents()
        {
            BotSprintingController = new Models.BotSprintingController(botOwner);
            BotPath = new BotPathData(botOwner);

            BotMonitor = BotMonitorController.GetBotMonitorController(botOwner);
            yield return null;

            IdentityData = BotIdentityData.GetBotIdentityData(botOwner);
            yield return null;

            QuestSelector = BotQuestSelector.GetBotQuestSelector(botOwner);
            yield return null;

            isInitialized = true;
            isInitializing = false;
        }

        protected void Update()
        {
            // Fix for this component not being destroyed when raids end. This can happen when exceptions are ignored while destroying bots.
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if ((botOwner.BotState != EBotState.Active) || botOwner.IsDead)
            {
                return;
            }

            if (!isInitialized)
            {
                return;
            }

            if (!HasInitialQuestBeenSelected)
            {
                SetInitialQuest();
                return;
            }

            if (!IsQuestingAllowed)
            {
                return;
            }

            if (BotRegistrationManager.IsBotSleeping(botOwner.Profile.Id))
            {
                timeSpentAtObjectiveTimer.Stop();
                return;
            }

            // Don't allow expensive parts of this behavior (selecting an objective) to run too often
            if (!canUpdate())
            {
                return;
            }

            if (IsCloseToObjective())
            {
                timeSpentAtObjectiveTimer.Start();
            }
            else
            {
                timeSpentAtObjectiveTimer.Reset();
            }

            if (QuestSelector.NewAssignmentReady)
            {
                BotJobAssignment? botJobAssignment = botOwner.GetMostRecentJobAssignment();
                if (botJobAssignment != null)
                {
                    SetObjective(botJobAssignment);
                }
            }

            // Don't monitor the bot's job assignment if it's a follower of a boss
            if (BotHiveMindMonitor.HasGroupLeader(botOwner) && !PrioritizeQuestingOverFollowing)
            {
                return;
            }

            bool? hasWaitedLongEnough = CurrentAssignment?.HasWaitedLongEnoughAfterEnding();
            if (hasWaitedLongEnough != true)
            {
                return;
            }

            if (botOwner.NumberOfConsecutiveFailedAssignments() >= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxCount)
            {
                Singleton<LoggingUtil>.Instance.LogWarning(botOwner.GetText() + " has failed too many consecutive assignments and is no longer allowed to quest.");
                botOwner.Mover.Stop();
                IsQuestingAllowed = false;
                return;
            }

            //Singleton<LoggingUtil>.Instance.LogDebug("Refreshing job assignment for " + botOwner.GetText());
            QuestSelector.RefreshJobAssignment();
        }

        private void SetInitialQuest()
        {
            if (!IdentityData.ActivationComplete)
            {
                return;
            }

            if (!Singleton<GameWorld>.Instance.TryGetComponent(out BotQuestBuilder botQuestBuilder) || !botQuestBuilder.HaveQuestsBeenBuilt)
            {
                return;
            }

            IsQuestingAllowed = IdentityData.BotType.AllowsQuesting();
            if (IdentityData.BotType == BotType.Undetermined)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not determine bot type for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")");
            }

            if (IsQuestingAllowed)
            {
                QuestSelector.RefreshJobAssignment();
            }

            HasInitialQuestBeenSelected = true;
        }

        public BotJobAssignment? CloneCurrentJobAssignment(BotOwner otherBotToDoAssignment)
        {
            if (CurrentAssignment == null)
            {
                return null;
            }

            BotJobAssignment clonedAssignment = new BotJobAssignment(otherBotToDoAssignment, CurrentAssignment);
            BotJobAssignmentController.Register(clonedAssignment);

            return clonedAssignment;
        }

        public void SetObjective(BotJobAssignment objective)
        {
            if (objective == CurrentAssignment)
            {
                return;
            }

            lastAssignment = CurrentAssignment;
            CurrentAssignment = objective;
            QuestSelector.AcceptNewAssignment();

            if ((CurrentAssignment != null) && QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " is now doing " + CurrentAssignment.ToString());
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Bot " + botOwner.GetText() + " was given a null job assignment");
            }

            if ((lastAssignment != null) && QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + botOwner.GetText() + " was previously doing " + lastAssignment.ToString());
            }

            //double? timeSinceBotStartedQuest = lastAssignment.QuestAssignment.ElapsedTimeSinceBotStarted(bot);
            //double? timeSinceBotLastFinishedQuest = lastAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(bot);
            //string startedTimeText = timeSinceBotStartedQuest.HasValue ? timeSinceBotStartedQuest.Value.ToString() : "N/A";
            //string lastFinishedTimeText = timeSinceBotLastFinishedQuest.HasValue ? timeSinceBotLastFinishedQuest.Value.ToString() : "N/A";
            //Singleton<LoggingUtil>.Instance.LogInfo("Time since first objective ended: " + startedTimeText + ", Time since last objective ended: " + lastFinishedTimeText);

            CheckIfQuestAssignmentIsOnLightkeeperIsland();
        }

        private void CheckIfQuestAssignmentIsOnLightkeeperIsland()
        {
            if (!Singleton<GameWorld>.Instance.TryGetComponent(out Components.LightkeeperIslandMonitor lightkeeperIslandMonitor))
            {
                return;
            }

            if (!lightkeeperIslandMonitor.IsBotObjectiveOnLightkeeperIsland(botOwner))
            {
                return;
            }

            if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + "'s new quest assignment is on Lightkeeper Island");
            }
        }

        public void CompleteObjective()
        {
            if (CurrentAssignment == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot complete a null assignment for " + botOwner.GetText());
                return;
            }

            CurrentAssignment.Complete();

            BotPath.ClearPath();

            float duration = (float)WaitTimeAfterCompleting!.Value + 5;
            UpdateLootingBehavior(CurrentAssignment.QuestObjectiveAssignment.LootAfterCompletingSetting, duration);

            foreach (BotOwner follower in BotLogic.HiveMind.BotHiveMindMonitor.GetGroupFollowers(botOwner))
            {
                BotObjectiveManager? followerObjectiveManager = follower.GetObjectiveManager();
                if (followerObjectiveManager == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Could not get BotObjectiveManager component for follower " + follower.GetText() + " of " + botOwner.GetText());
                    continue;
                }

                followerObjectiveManager.UpdateLootingBehavior(CurrentAssignment.QuestObjectiveAssignment.LootAfterCompletingSetting, duration);
            }

            StuckCount = 0;
        }

        public void FailObjective()
        {
            if (CurrentAssignment == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot fail a null assignment for " + botOwner.GetText());
                return;
            }

            CurrentAssignment.Fail();
        }

        public bool TryChangeObjective()
        {
            if (botOwner == null)
            {
                return false;
            }

            double? timeSinceJobEnded = CurrentAssignment?.TimeSinceEnded();
            if (timeSinceJobEnded.HasValue && (timeSinceJobEnded.Value < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.MinTimeBetweenSwitchingObjectives))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not change the job assignment for " + botOwner.GetText() + " because not enough time has elapsed since it was last updated");
                return false;
            }

            CurrentAssignment?.Inactivate();

            return QuestSelector.TryCreateNewJobAssignment();
        }

        public void UnlockDoor(EFT.Interactive.WorldInteractiveObject door)
        {
            CurrentAssignment?.SetDoorToUnlock(door);
        }

        public void DoorIsUnlocked()
        {
            CurrentAssignment?.DoorIsUnlocked();
        }

        public void StopQuesting()
        {
            IsQuestingAllowed = false;

            if (QuestingBotsPluginConfig.VerboseLogging.Value.HasFlag(VerboseLoggingType.QuestingActions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " is no longer allowed to quest.");
            }
        }

        public void ReportIncompletePath()
        {
            if (CurrentAssignment == null)
            {
                return;
            }

            CurrentAssignment.HasCompletePath = false;
        }

        public void RetryPath()
        {
            if (CurrentAssignment == null)
            {
                return;
            }

            CurrentAssignment.HasCompletePath = true;
        }

        public bool CanSprintToObjective()
        {
            if (CurrentAssignment?.QuestObjectiveAssignment != null)
            {
                if (DistanceToObjective < QuestingBotsPluginConfig.MinSprintingDistance.Value)
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (DistanceToObjective < CurrentAssignment.QuestObjectiveAssignment.MaxRunDistance)
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (!CurrentAssignment.QuestAssignment.CanRunBetweenObjectives && (CurrentAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(botOwner) > 0))
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " can no longer run for quest " + targetQuest.Name);
                    return false;
                }
            }

            if (lastAssignment?.QuestObjectiveAssignment != null)
            {
                if (DistanceFromLastObjective < lastAssignment.QuestObjectiveAssignment.MaxRunDistance)
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to its last objective, " + assignment.Position.ToString());
                    return false;
                }
            }

            return true;
        }

        public bool IsAllowedToTakeABreak()
        {
            switch (CurrentQuestAction)
            {
                case QuestAction.HoldAtPosition:
                case QuestAction.Teleport:
                case QuestAction.Ambush:
                case QuestAction.Snipe:
                case QuestAction.CloseNearbyDoors:
                case QuestAction.OpenNearbyDoors:
                case QuestAction.ToggleSwitch:
                    return false;
                case QuestAction.PlantItem:
                    return !IsCloseToObjective();
            }

            return true;
        }

        public bool IsAllowedToInvestigate()
        {
            switch (CurrentQuestAction)
            {
                case QuestAction.Teleport:
                case QuestAction.Ambush:
                case QuestAction.CloseNearbyDoors:
                case QuestAction.OpenNearbyDoors:
                case QuestAction.ToggleSwitch:
                    return false;
                case QuestAction.PlantItem:
                    return !IsCloseToObjective();
            }

            return true;
        }

        public EFT.Interactive.WorldInteractiveObject GetCurrentQuestInteractiveObject()
        {
            if (MustUnlockDoor)
            {
                return CurrentAssignment?.DoorToUnlock!;
            }

            return CurrentAssignment?.QuestObjectiveStepAssignment?.InteractiveObject!;
        }

        public bool DoesBotWantToExtract()
        {
            BotExtractMonitor? botExtractMonitor = BotMonitor?.GetMonitor<BotExtractMonitor>();
            if (botExtractMonitor == null)
            {
                return false;
            }

            if (botExtractMonitor.IsTryingToExtract)
            {
                return true;
            }

            if (botExtractMonitor.IsBotReadyToExtract && botExtractMonitor.TryInstructBotToExtract())
            {
                StopQuesting();
                return true;
            }

            return false;
        }

        public void UpdateLootingBehavior(LootAfterCompleting behavior, float duration = 0)
        {
            switch (behavior)
            {
                case LootAfterCompleting.Force:
                    BotMonitor.GetMonitor<BotLootingMonitor>().TryForceBotToScanLoot();
                    break;
                case LootAfterCompleting.Inhibit:
                    BotMonitor.GetMonitor<BotLootingMonitor>().TryPreventBotFromLooting(duration);
                    break;
            }
        }
    }
}
