using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class BotObjectiveManager : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        public bool IsInitialized { get; private set; } = false;
        public bool IsQuestingAllowed { get; private set; } = false;
        public bool CanRushPlayerSpawn { get; private set; } = false;
        public int StuckCount { get; set; } = 0;
        public float PauseRequest { get; set; } = 0;
        public BotMonitor BotMonitor { get; private set; } = null;
        public EFT.Interactive.Door DoorToOpen { get; set; } = null;

        private BotOwner botOwner = null;
        private BotJobAssignment assignment = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();

        public Vector3? Position => assignment?.Position;
        public bool IsJobAssignmentActive => assignment?.IsActive == true;
        public bool HasCompletePath => assignment.HasCompletePath;
        public bool MustUnlockDoor => assignment?.DoorToUnlock != null;
        public QuestAction CurrentQuestAction => assignment?.QuestObjectiveStepAssignment?.ActionType ?? QuestAction.Undefined;
        public double MinElapsedActionTime => assignment?.QuestObjectiveStepAssignment?.MinElapsedTime ?? 0;
        public float ChanceOfHavingKey => assignment?.QuestObjectiveStepAssignment?.ChanceOfHavingKey ?? 0;

        public double TimeSpentAtObjective => timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0;
        public float DistanceToObjective => Position.HasValue ? Vector3.Distance(Position.Value, botOwner.Position) : float.NaN;

        public bool IsCloseToObjective(float distance) => DistanceToObjective <= distance;
        public bool IsCloseToObjective() => IsCloseToObjective(ConfigController.Config.Questing.BotSearchDistances.OjectiveReachedIdeal);

        public void StartJobAssigment() => assignment.Start();
        public void ReportIncompletePath() => assignment.HasCompletePath = false;
        public void RetryPath() => assignment.HasCompletePath = true;

        public static BotObjectiveManager GetObjectiveManagerForBot(BotOwner bot)
        {
            if (bot == null)
            {
                return null;
            }

            if (!bot.isActiveAndEnabled || bot.IsDead)
            {
                return null;
            }

            Player botPlayer = bot.GetPlayer;
            if (botPlayer == null)
            {
                return null;
            }

            GameObject obj = botPlayer.gameObject;
            if (obj == null)
            {
                return null;
            }

            if (obj.TryGetComponent(out BotObjectiveManager objectiveManager))
            {
                return objectiveManager;
            }

            return null;
        }

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
            BotMonitor = new BotMonitor(botOwner);
        }

        private void updateBotType()
        {
            if (!HiveMind.BotHiveMindMonitor.IsRegistered(botOwner))
            {
                return;
            }

            BotType botType = BotRegistrationManager.GetBotType(botOwner);

            if ((botType == BotType.PMC) && ConfigController.Config.Questing.AllowedBotTypesForQuesting.PMC)
            {
                CanRushPlayerSpawn = BotGenerator.IsBotFromInitialPMCSpawns(botOwner);
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

            // Only set an objective for the bot if its type is allowed to spawn and all quests have been loaded and generated
            if (IsQuestingAllowed && BotQuestBuilder.HaveQuestsBeenBuilt)
            {
                LoggingController.LogInfo("Setting objective for " + botType.ToString() + " " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")...");
                assignment = botOwner.GetCurrentJobAssignment();
            }

            if (botType == BotType.Undetermined)
            {
                LoggingController.LogError("Could not determine bot type for " + botOwner.GetText() + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")");
                return;
            }

            IsInitialized = true;
        }

        private void Update()
        {
            if (!BotQuestBuilder.HaveQuestsBeenBuilt)
            {
                return;
            }

            if (!IsInitialized)
            {
                updateBotType();
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

            if (IsCloseToObjective())
            {
                timeSpentAtObjectiveTimer.Start();
            }
            else
            {
                timeSpentAtObjectiveTimer.Reset();
            }

            // Don't allow expensive parts of this behavior (selecting an objective) to run too often
            if (!canUpdate())
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

                assignment = botOwner.GetCurrentJobAssignment();
            }
        }

        public void CompleteObjective()
        {
            assignment.Complete();

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

            assignment = botOwner.GetNewBotJobAssignment();
            LoggingController.LogInfo("Bot " + botOwner.GetText() + " is now doing " + assignment.ToString());

            return true;
        }

        public void UnlockDoor(EFT.Interactive.Door door)
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
            if (assignment == null)
            {
                return true;
            }

            if ((assignment.QuestObjectiveAssignment != null) && (DistanceToObjective < assignment.QuestObjectiveAssignment.MaxRunDistance))
            {
                //LoggingController.LogInfo("Bot " + botOwner.GetText() + " will stop running because it's too close to " + targetObjective.ToString());
                return false;
            }

            if 
            (
                (assignment.QuestAssignment != null)
                && !assignment.QuestAssignment.CanRunBetweenObjectives
                && (assignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(botOwner) > 0)
            )
            {
                //LoggingController.LogInfo("Bot " + botOwner.GetText() + " can no longer run for quest " + targetQuest.Name);
                return false;
            }

            return true;
        }

        public bool IsAllowedToTakeABreak()
        {
            if ((CurrentQuestAction == QuestAction.PlantItem) && IsCloseToObjective())
            {
                return false;
            }

            if (CurrentQuestAction == QuestAction.ToggleSwitch)
            {
                return false;
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
    }
}
