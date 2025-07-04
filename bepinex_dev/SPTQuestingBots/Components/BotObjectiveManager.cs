﻿using Comfort.Common;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models.Pathing;
using SPTQuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Components
{
    public class BotObjectiveManager : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        public bool IsInitialized { get; private set; } = false;
        public bool IsQuestingAllowed { get; private set; } = false;
        public int StuckCount { get; set; } = 0;
        public float PauseRequest { get; set; } = 0;
        public Models.BotSprintingController BotSprintingController { get; private set; } = null;
        public BotLogic.BotMonitor.BotMonitorController BotMonitor { get; private set; } = null;
        public BotPathData BotPath { get; private set; } = null;
        public EFT.Interactive.Door DoorToOpen { get; set; } = null;
        public Vector3? LastCorner { get; set; } = null;

        private BotOwner botOwner = null;
        private BotJobAssignment assignment = null;
        private BotJobAssignment lastAssignment = null;
        private ExfiltrationPoint exfiltrationPoint = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();
        private Components.BotQuestBuilder botQuestBuilder = null;

        public Vector3? Position => assignment?.Position;
        public Vector3? LookToPosition => assignment?.LookToPosition;
        public bool IsJobAssignmentActive => assignment?.IsActive == true;
        public bool HasCompletePath => assignment.HasCompletePath;
        public string DoorIDToUnlockForObjective => assignment?.QuestObjectiveAssignment?.DoorIDToUnlock ?? "";
        public Vector3? InteractionPositionForDoorToUnlockForObjective => assignment?.QuestObjectiveAssignment?.InteractionPositionToUnlockDoor?.ToUnityVector3();
        public bool MustUnlockDoor => assignment?.DoorToUnlock != null;
        public QuestAction CurrentQuestAction => assignment?.QuestObjectiveStepAssignment?.ActionType ?? QuestAction.Undefined;
        public double MinElapsedActionTime => assignment?.MinElapsedTime ?? 0;
        public float ChanceOfHavingKey => assignment?.QuestObjectiveStepAssignment?.ChanceOfHavingKey ?? 0;
        public float? MaxDistanceForCurrentStep => assignment?.QuestObjectiveStepAssignment?.MaxDistance;

        public double TimeSpentAtObjective => timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0;
        public float DistanceToObjective => Position.HasValue ? Vector3.Distance(Position.Value, botOwner.Position) : float.NaN;
        public float DistanceFromLastObjective => (lastAssignment?.Position != null) ? Vector3.Distance(lastAssignment.Position.Value, botOwner.Position) : float.MaxValue;

        public bool IsCloseToObjective(float distance) => DistanceToObjective <= distance;
        public bool IsCloseToObjective() => IsCloseToObjective(ConfigController.Config.Questing.BotSearchDistances.OjectiveReachedIdeal);

        public void StartJobAssigment() => assignment.Start();
        public void ReportIncompletePath() => assignment.HasCompletePath = false;
        public void RetryPath() => assignment.HasCompletePath = true;

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
            botOwner.Settings.FileSettings.Mind.AVOID_BTR_RADIUS_SQR = ConfigController.Config.Questing.BTRRunDistance * ConfigController.Config.Questing.BTRRunDistance;
        }

        private void updateBotType()
        {
            if (!BotLogic.HiveMind.BotHiveMindMonitor.IsRegistered(botOwner))
            {
                LoggingController.LogError(botOwner.GetText() + " has not been registered in BotHiveMindMonitor");
            }

            BotType botType = Controllers.BotRegistrationManager.GetBotType(botOwner);

            if ((botType == BotType.PMC) && ConfigController.Config.Questing.AllowedBotTypesForQuesting.PMC)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.Boss) && ConfigController.Config.Questing.AllowedBotTypesForQuesting.Boss)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.Scav) && ConfigController.Config.Questing.AllowedBotTypesForQuesting.Scav)
            {
                IsQuestingAllowed = true;
            }
            if ((botType == BotType.PScav) && ConfigController.Config.Questing.AllowedBotTypesForQuesting.PScav)
            {
                IsQuestingAllowed = true;
            }

            if (botType == BotType.Undetermined)
            {
                LoggingController.LogError("Could not determine bot type for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")");
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
            if (BotHiveMindMonitor.HasBoss(botOwner))
            {
                return;
            }

            bool? hasWaitedLongEnough = assignment?.HasWaitedLongEnoughAfterEnding();
            if (hasWaitedLongEnough.HasValue && hasWaitedLongEnough.Value)
            {
                if (botOwner.NumberOfConsecutiveFailedAssignments() >= ConfigController.Config.Questing.StuckBotDetection.MaxCount)
                {
                    LoggingController.LogWarning(botOwner.GetText() + " has failed too many consecutive assignments and is no longer allowed to quest.");
                    botOwner.Mover.Stop();
                    IsQuestingAllowed = false;
                    return;
                }

                SetObjective(botOwner.GetCurrentJobAssignment());
            }
        }

        public void SetObjective(BotJobAssignment objective)
        {
            if (objective == assignment)
            {
                return;
            }

            lastAssignment = assignment;
            assignment = objective;

            //LoggingController.LogInfo("Updated objective for " + botOwner.GetText() + " from " + (lastAssignment?.ToString() ?? "[None]") + " to " + assignment.ToString());

            if (!Singleton<GameWorld>.Instance.TryGetComponent(out Components.LightkeeperIslandMonitor lightkeeperIslandMonitor))
            {
                return;
            }

            if (lightkeeperIslandMonitor.IsBotObjectiveOnLightkeeperIsland(botOwner))
            {
                LoggingController.LogInfo(botOwner.GetText() + "'s new quest assignment is on Lightkeeper Island");
            }
        }

        public void CompleteObjective()
        {
            assignment.Complete();

            BotPath.ClearPath();

            float duration = (float)assignment.QuestObjectiveStepAssignment.WaitTimeAfterCompleting + 5;
            UpdateLootingBehavior(assignment.QuestObjectiveAssignment.LootAfterCompletingSetting, duration);

            StuckCount = 0;
        }

        public void FailObjective()
        {
            assignment.Fail();
        }

        public bool TryChangeObjective()
        {
            double? timeSinceJobEnded = assignment?.TimeSinceEnded();
            if (timeSinceJobEnded.HasValue && (timeSinceJobEnded.Value < ConfigController.Config.Questing.MinTimeBetweenSwitchingObjectives))
            {
                return false;
            }

            assignment?.Inactivate();

            SetObjective(botOwner?.GetNewBotJobAssignment());
            LoggingController.LogInfo("Bot " + botOwner.GetText() + " is now doing " + (assignment?.ToString() ?? "[NULL]"));

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
            LoggingController.LogInfo(botOwner.GetText() + " is no longer allowed to quest.");
        }

        public bool CanSprintToObjective()
        {
            if (assignment?.QuestObjectiveAssignment != null)
            {
                if (DistanceToObjective < QuestingBotsPluginConfig.MinSprintingDistance.Value)
                {
                    //LoggingController.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (DistanceToObjective < assignment.QuestObjectiveAssignment.MaxRunDistance)
                {
                    //LoggingController.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + assignment.Position.ToString());
                    return false;
                }

                if (!assignment.QuestAssignment.CanRunBetweenObjectives && (assignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(botOwner) > 0))
                {
                    //LoggingController.LogInfo("Bot " + botOwner.GetText() + " can no longer run for quest " + targetQuest.Name);
                    return false;
                }
            }

            if (lastAssignment?.QuestObjectiveAssignment != null)
            {
                if (DistanceFromLastObjective < lastAssignment.QuestObjectiveAssignment.MaxRunDistance)
                {
                    //LoggingController.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to its last objective, " + assignment.Position.ToString());
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
                case QuestAction.Ambush:
                case QuestAction.Snipe:
                case QuestAction.CloseNearbyDoors:
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
                case QuestAction.Ambush:
                case QuestAction.CloseNearbyDoors:
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
                return assignment?.DoorToUnlock;
            }

            return assignment?.QuestObjectiveStepAssignment?.InteractiveObject;
        }

        public bool DoesBotWantToExtract()
        {
            BotExtractMonitor botExtractMonitor = BotMonitor?.GetMonitor<BotExtractMonitor>();
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

                //LoggingController.LogInfo(botOwner.GetText() + " has selected " + furthestPoint.Key.Settings.Name + " as its furthest exfil point (" + furthestPoint.Value + "m)");
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
                LoggingController.LogInfo("Setting objective for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")...");
                try
                {
                    SetObjective(botOwner.GetCurrentJobAssignment());
                }
                catch (TimeoutException)
                {
                    LoggingController.LogError("Timed out when trying to select an initial objective for " + botOwner.GetText());
                }
            }
        }
    }
}
