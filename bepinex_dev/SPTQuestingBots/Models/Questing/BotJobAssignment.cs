using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public enum JobAssignmentStatus
    {
        NotStarted,
        Pending,
        Active,
        Completed,
        Failed,
        Archived,
    }

    public class BotJobAssignment : JobAssignment
    {
        public JobAssignmentStatus Status { get; private set; } = JobAssignmentStatus.NotStarted;
        public BotOwner BotOwner { get; private set; }
        public string BotName { get; private set; } = "???";
        public string BotNickname { get; private set; } = "???";
        public int BotLevel { get; private set; } = -1;
        public WorldInteractiveObject DoorToUnlock { get; private set; } = null;
        public DateTime? StartTime { get; private set; } = null;
        public DateTime? EndTime { get; private set; } = null;
        public bool HasCompletePath { get; set; } = true;
        public double MinElapsedTime { get; private set; } = 0;

        public bool IsActive => Status == JobAssignmentStatus.Active || Status == JobAssignmentStatus.Pending;
        public bool IsCompletedOrArchived => Status == JobAssignmentStatus.Completed || Status == JobAssignmentStatus.Archived;
        public Vector3? LookToPosition => QuestObjectiveStepAssignment?.GetLookToPosition();

        public BotJobAssignment(BotOwner bot)
        {
            BotOwner = bot;
            updateBotInfo();
        }

        public BotJobAssignment(BotOwner bot, Quest quest, QuestObjective objective) : this(bot)
        {
            QuestAssignment = quest;
            QuestObjectiveAssignment = objective;

            if (!TrySetNextObjectiveStep(true))
            {
                LoggingController.LogWarning("Unable to set first step for " + bot.GetText() + " for " + ToString());
            }
        }

        public double? TimeSinceStarted()
        {
            if (!StartTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - StartTime.Value).TotalMilliseconds / 1000.0;
        }

        public double? TimeSinceEnded()
        {
            if (!EndTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - EndTime.Value).TotalMilliseconds / 1000.0;
        }

        public bool HasWaitedLongEnoughAfterEnding()
        {
            return TimeSinceEnded() >= (QuestObjectiveStepAssignment?.WaitTimeAfterCompleting ?? 0);
        }

        public bool TrySetNextObjectiveStep(bool allowReset = false)
        {
            if ((Status != JobAssignmentStatus.Completed) && (Status != JobAssignmentStatus.NotStarted))
            {
                return false;
            }

            QuestObjectiveStep nextStep = QuestObjectiveAssignment.GetNextObjectiveStep(QuestObjectiveStepAssignment, allowReset);
            if (nextStep == null)
            {
                return false;
            }

            QuestObjectiveStepAssignment = nextStep;
            MinElapsedTime = nextStep.GetRandomMinElapsedTime();
            DoorToUnlock = null;
            EndTime = null;
            startInternal();

            return true;
        }

        public void Complete()
        {
            endInternal();

            if (Status != JobAssignmentStatus.Completed)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has completed " + ToString());
            }

            Status = JobAssignmentStatus.Completed;
        }

        public void Fail()
        {
            endInternal();

            if (Status != JobAssignmentStatus.Failed)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has failed " + ToString());
            }

            Status = JobAssignmentStatus.Failed;
        }

        public void Start()
        {
            Status = JobAssignmentStatus.Active;
        }

        public void Inactivate()
        {
            if (Status == JobAssignmentStatus.Active)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is no longer doing " + ToString());

                Status = JobAssignmentStatus.Pending;
            }
        }

        public void Archive()
        {
            Status = JobAssignmentStatus.Archived;
        }

        public void SetDoorToUnlock(WorldInteractiveObject door)
        {
            DoorToUnlock = door;
            HasCompletePath = true;
        }

        public void DoorIsUnlocked()
        {
            if (DoorToUnlock == null)
            {
                return;
            }

            DoorToUnlock = null;
            HasCompletePath = true;
        }

        private void startInternal()
        {
            if (!StartTime.HasValue)
            {
                StartTime = DateTime.Now;
            }

            Status = JobAssignmentStatus.Pending;
            HasCompletePath = true;
        }

        private void endInternal()
        {
            if (!EndTime.HasValue)
            {
                EndTime = DateTime.Now;
            }

            DoorToUnlock = null;
        }

        private void updateBotInfo()
        {
            if (BotOwner == null)
            {
                throw new InvalidOperationException("Cannot update info for a null bot");
            }

            BotName = BotOwner.name;
            BotNickname = BotOwner.Profile.Nickname;
            BotLevel = BotOwner.Profile.Info.Level;
        }
    }
}
