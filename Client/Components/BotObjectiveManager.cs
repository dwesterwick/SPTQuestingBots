using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Pathing;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using System;
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
        public bool IsInitialized { get; private set; } = false;
        public bool IsQuestingAllowed { get; private set; } = false;
        public int StuckCount { get; set; } = 0;
        public float PauseRequest { get; set; } = 0;
        public Models.BotSprintingController BotSprintingController { get; private set; } = null!;
        public BotLogic.BotMonitor.BotMonitorController BotMonitor { get; private set; } = null!;
        public BotPathData BotPath { get; private set; } = null!;
        public EFT.Interactive.Door DoorToOpen { get; set; } = null!;
        public Vector3? LastCorner { get; set; } = null;

        private BotOwner botOwner = null!;
        private BotJobAssignment assignment = null!;
        private BotJobAssignment lastAssignment = null!;
        private ExfiltrationPoint exfiltrationPoint = null!;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();
        private Components.BotQuestBuilder botQuestBuilder = null!;

        public Vector3? Position => assignment?.Position;
        public Vector3? LookToPosition => assignment?.LookToPosition;
        public Vector3? TargetPosition => assignment?.TargetPosition;
        public bool IsJobAssignmentActive => assignment?.IsActive == true;
        public bool HasTeleportingAssignment => assignment?.MustTeleport == true;
        public bool HasCompletePath => assignment.HasCompletePath;
        public string DoorIDToUnlockForObjective => assignment?.QuestObjectiveAssignment?.DoorIDToUnlock ?? "";
        public Vector3? InteractionPositionForDoorToUnlockForObjective => assignment?.QuestObjectiveAssignment?.InteractionPositionToUnlockDoor?.ToUnityVector3();
        public bool MustUnlockDoor => assignment?.DoorToUnlock != null;
        public QuestAction CurrentQuestAction => assignment?.QuestObjectiveStepAssignment?.ActionType ?? QuestAction.Undefined;
        public double MinElapsedActionTime => assignment?.MinElapsedTime ?? 0;
        public float ChanceOfHavingKey => assignment?.QuestObjectiveStepAssignment?.ChanceOfHavingKey ?? 0;
        public float? MaxDistanceForCurrentStep => assignment?.QuestObjectiveStepAssignment?.MaxDistance;
        public bool IgnoreHearing => assignment?.IgnoreHearing ?? false;
        public bool ForceUnlock => assignment?.ForceUnlock ?? false;
        public bool PrioritizeQuestingOverFollowing => assignment?.PrioritizeOverFollowing ?? false;
        public double? WaitTimeAfterCompleting => assignment?.QuestObjectiveStepAssignment?.WaitTimeAfterCompleting;

        public double TimeSpentAtObjective => timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0;
        public float DistanceToObjective => Position.HasValue ? Vector3.Distance(Position.Value, botOwner.Position) : float.NaN;
        public float DistanceFromLastObjective => (lastAssignment?.Position != null) ? Vector3.Distance(lastAssignment.Position.Value, botOwner.Position) : float.MaxValue;

        public bool IsCloseToObjective(float distance) => DistanceToObjective <= distance;
        public bool IsCloseToObjective() => IsCloseToObjective(Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotSearchDistances.OjectiveReachedIdeal);

        public void StartJobAssigment() => assignment.Start();
        public void ReportIncompletePath() => assignment.HasCompletePath = false;
        public void RetryPath() => assignment.HasCompletePath = true;

        public double? TimeSinceJobAssigmentStarted() => assignment.TimeSinceStarted();

        public override string ToString()
        {
            if (assignment.QuestAssignment != null)
            {
                return assignment.ToString();
            }

            return "Position " + (Position?.ToString() ?? "???");
        }

        public void Init(BotOwner _botOwner)
        {
            if (IsInitialized)
            {
                return;
            }

            base.UpdateInterval = 200;
            botOwner = _botOwner;

            if (BotSprintingController == null)
            {
                BotSprintingController = new Models.BotSprintingController(botOwner);
            }

            if (BotMonitor == null)
            {
                BotMonitor = botOwner.GetPlayer.gameObject.GetOrAddComponent<BotLogic.BotMonitor.BotMonitorController>();
                BotMonitor.Init(botOwner);
            }

            if (BotPath == null)
            {
                BotPath = new BotPathData(botOwner);
            }

            if (exfiltrationPoint == null)
            {
                SetExfiliationPointForQuesting();
            }

            // Override the EFT distance that makes bots "avoid danger" when the BTR is near
            botOwner.Settings.FileSettings.Mind.AVOID_BTR_RADIUS_SQR = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BTRRunDistance * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BTRRunDistance;
        }

        private void updateBotType()
        {
            if (!BotLogic.HiveMind.BotHiveMindMonitor.IsRegistered(botOwner))
            {
                Singleton<LoggingUtil>.Instance.LogError(botOwner.GetText() + " has not been registered in BotHiveMindMonitor");
            }

            BotType botType = Controllers.BotRegistrationManager.GetBotType(botOwner);

            if ((botType == BotType.PMC) && Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.AllowedBotTypesForQuesting.PMC)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.Boss) && Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.AllowedBotTypesForQuesting.Boss)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.Scav) && Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.AllowedBotTypesForQuesting.Scav)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.PScav) && Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.AllowedBotTypesForQuesting.PScav)
            {
                IsQuestingAllowed = true;
            }

            if (botType == BotType.Undetermined)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not determine bot type for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")");
            }

            IsInitialized = true;
        }

        protected void Update()
        {
            // Fix for this component not being destroyed when raids end. This can happen when exceptions are ignored while destroying bots.
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if ((botQuestBuilder == null) && !Singleton<GameWorld>.Instance.TryGetComponent(out botQuestBuilder))
            {
                return;
            }

            if (!botQuestBuilder.HaveQuestsBeenBuilt)
            {
                return;
            }

            if (!IsInitialized)
            {
                updateBotType();
                setInitialObjective();

                return;
            }

            if (!IsQuestingAllowed)
            {
                return;
            }

            if ((botOwner.BotState != EBotState.Active) || botOwner.IsDead)
            {
                return;
            }

            bool isSleeping = BotRegistrationManager.IsBotSleeping(botOwner.Profile.Id);
            if (isSleeping)
            {
                timeSpentAtObjectiveTimer.Stop();
            }
            else if (IsCloseToObjective())
            {
                timeSpentAtObjectiveTimer.Start();
            }
            else
            {
                timeSpentAtObjectiveTimer.Reset();
            }

            // Don't allow expensive parts of this behavior (selecting an objective) to run too often
            if (isSleeping || !canUpdate())
            {
                return;
            }

            // Don't monitor the bot's job assignment if it's a follower of a boss
            if (BotHiveMindMonitor.HasBoss(botOwner) && !PrioritizeQuestingOverFollowing)
            {
                return;
            }

            bool? hasWaitedLongEnough = assignment?.HasWaitedLongEnoughAfterEnding();
            if (hasWaitedLongEnough.HasValue && hasWaitedLongEnough.Value)
            {
                if (botOwner.NumberOfConsecutiveFailedAssignments() >= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxCount)
                {
                    Singleton<LoggingUtil>.Instance.LogWarning(botOwner.GetText() + " has failed too many consecutive assignments and is no longer allowed to quest.");
                    botOwner.Mover.Stop();
                    IsQuestingAllowed = false;
                    return;
                }

                SetObjective(botOwner.GetCurrentJobAssignment());
            }
        }

        public BotJobAssignment CloneCurrentJobAssignment(BotOwner otherBotToDoAssignment)
        {
            BotJobAssignment clonedAssignment = new BotJobAssignment(otherBotToDoAssignment, assignment);
            BotJobAssignmentFactory.RegisterBotJobAssignment(clonedAssignment);

            return clonedAssignment;
        }

        public void SetObjective(BotJobAssignment objective)
        {
            if (objective == assignment)
            {
                return;
            }

            lastAssignment = assignment;
            assignment = objective;

            //Singleton<LoggingUtil>.Instance.LogInfo("Updated objective for " + botOwner.GetText() + " from " + (lastAssignment?.ToString() ?? "[None]") + " to " + assignment.ToString());

            if (!Singleton<GameWorld>.Instance.TryGetComponent(out Components.LightkeeperIslandMonitor lightkeeperIslandMonitor))
            {
                return;
            }

            if (lightkeeperIslandMonitor.IsBotObjectiveOnLightkeeperIsland(botOwner))
            {
                Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + "'s new quest assignment is on Lightkeeper Island");
            }
        }

        public void CompleteObjective()
        {
            assignment.Complete();

            BotPath.ClearPath();

            float duration = (float)WaitTimeAfterCompleting!.Value + 5;
            UpdateLootingBehavior(assignment.QuestObjectiveAssignment.LootAfterCompletingSetting, duration);

            foreach (BotOwner follower in BotLogic.HiveMind.BotHiveMindMonitor.GetFollowers(botOwner))
            {
                BotObjectiveManager? followerObjectiveManager = follower.GetObjectiveManager();
                if (followerObjectiveManager == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Could not get BotObjectiveManager component for follower " + follower.GetText() + " of " + botOwner.GetText());
                    continue;
                }

                followerObjectiveManager.UpdateLootingBehavior(assignment.QuestObjectiveAssignment.LootAfterCompletingSetting, duration);
            }

            StuckCount = 0;
        }

        public void FailObjective()
        {
            assignment.Fail();
        }

        public bool TryChangeObjective()
        {
            double? timeSinceJobEnded = assignment?.TimeSinceEnded();
            if (timeSinceJobEnded.HasValue && (timeSinceJobEnded.Value < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.MinTimeBetweenSwitchingObjectives))
            {
                return false;
            }

            assignment?.Inactivate();

            if (botOwner == null)
            {
                return false;
            }

            SetObjective(botOwner.GetNewBotJobAssignment());
            Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " is now doing " + (assignment?.ToString() ?? "[NULL]"));

            return true;
        }

        public void UnlockDoor(EFT.Interactive.WorldInteractiveObject door)
        {
            assignment.SetDoorToUnlock(door);
        }

        public void DoorIsUnlocked()
        {
            assignment.DoorIsUnlocked();
        }

        public void StopQuesting()
        {
            IsQuestingAllowed = false;
            Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " is no longer allowed to quest.");
        }

        public bool CanSprintToObjective()
        {
            if (assignment?.QuestObjectiveAssignment != null)
            {
                if (DistanceToObjective < QuestingBotsPluginConfig.MinSprintingDistance.Value)
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (DistanceToObjective < assignment.QuestObjectiveAssignment.MaxRunDistance)
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (!assignment.QuestAssignment.CanRunBetweenObjectives && (assignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(botOwner) > 0))
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
                return assignment?.DoorToUnlock!;
            }

            return assignment?.QuestObjectiveStepAssignment?.InteractiveObject!;
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

        public void SetExfiliationPointForQuesting()
        {
            Dictionary<ExfiltrationPoint, float> exfiltrationPointDistances = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints
                .ToDictionary(p => p, p => Vector3.Distance(p.transform.position, botOwner.Position));

            if (exfiltrationPointDistances.Count > 0)
            {
                KeyValuePair<ExfiltrationPoint, float> furthestPoint = exfiltrationPointDistances
                    .OrderBy(p => p.Value)
                    .Last();

                exfiltrationPoint = furthestPoint.Key;

                //Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " has selected " + furthestPoint.Key.Settings.Name + " as its furthest exfil point (" + furthestPoint.Value + "m)");
            }
        }

        public float? DistanceToExfiltrationPointForQuesting()
        {
            if (exfiltrationPoint == null)
            {
                return null;
            }

            return Vector3.Distance(botOwner.Position, exfiltrationPoint.transform.position);
        }

        public Vector3? VectorToExfiltrationPointForQuesting()
        {
            if (exfiltrationPoint == null)
            {
                return null;
            }

            return exfiltrationPoint.transform.position - botOwner.Position;
        }

        private void setInitialObjective()
        {
            // Only set an objective for the bot if its type is allowed to spawn and all quests have been loaded and generated
            if (IsQuestingAllowed && Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Setting objective for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")...");
                try
                {
                    SetObjective(botOwner.GetCurrentJobAssignment());
                }
                catch (TimeoutException)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Timed out when trying to select an initial objective for " + botOwner.GetText());
                }
            }
        }
    }
}
