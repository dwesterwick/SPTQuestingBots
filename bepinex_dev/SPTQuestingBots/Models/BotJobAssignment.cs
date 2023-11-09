using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public enum JobAssignmentStatus
    {
        NotStarted,
        Pending,
        Active,
        Completed,
        Failed,
        Archived
    }

    public class BotJobAssignment
    {
        public JobAssignmentStatus Status { get; private set; } = JobAssignmentStatus.NotStarted;
        public BotOwner BotOwner { get; private set; }
        public string BotName { get; private set; } = "???";
        public string BotNickname { get; private set; } = "???";
        public int BotLevel { get; private set; } = -1;
        public Quest QuestAssignment { get; private set; } = null;
        public QuestObjective QuestObjectiveAssignment { get; private set; } = null;
        public QuestObjectiveStep QuestObjectiveStepAssignment { get; private set; } = null;
        public DateTime? StartTime { get; private set; } = null;
        public DateTime? EndTime { get; private set; } = null;
        public bool HasCompletePath { get; set; } = true;

        public bool IsActive => Status == JobAssignmentStatus.Active || Status == JobAssignmentStatus.Pending;
        public Vector3? Position => QuestObjectiveStepAssignment?.GetPosition() ?? null;

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

        public override string ToString()
        {
            string stepNumberText = QuestObjectiveStepAssignment?.StepNumber?.ToString() ?? "???";
            return "Step #" + stepNumberText + " for objective " + (QuestObjectiveAssignment?.ToString() ?? "???") + " in quest " + QuestAssignment.Name;
        }

        public double? TimeSinceAssignment()
        {
            if (!StartTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - StartTime.Value).TotalMilliseconds / 1000.0;
        }

        public double? TimeSinceJobEnded()
        {
            if (!EndTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - EndTime.Value).TotalMilliseconds / 1000.0;
        }

        public bool HasWaitedLongEnoughAfterEnding()
        {
            return TimeSinceJobEnded() >= (QuestObjectiveStepAssignment?.WaitTimeAfterCompleting ?? 0);
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
            EndTime = null;
            startJobAssingment();

            return true;
        }

        public void CompleteJobAssingment()
        {
            endJobAssingment();

            if (Status != JobAssignmentStatus.Completed)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has completed " + ToString());
            }

            Status = JobAssignmentStatus.Completed;
        }

        public void FailJobAssingment()
        {
            endJobAssingment();

            if (Status != JobAssignmentStatus.Failed)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has failed " + ToString());
            }

            Status = JobAssignmentStatus.Failed;
        }

        public void StartJobAssignment()
        {
            Status = JobAssignmentStatus.Active;
        }

        public void InactivateJobAssignment()
        {
            if (Status == JobAssignmentStatus.Active)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is no longer doing " + ToString());

                Status = JobAssignmentStatus.Pending;
            }
        }

        public void ArchiveJobAssignment()
        {
            Status = JobAssignmentStatus.Archived;
        }

        private void startJobAssingment()
        {
            if (!StartTime.HasValue)
            {
                StartTime = DateTime.Now;
            }

            Status = JobAssignmentStatus.Pending;
            HasCompletePath = true;
        }

        private void endJobAssingment()
        {
            if (!EndTime.HasValue)
            {
                EndTime = DateTime.Now;
            }
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
