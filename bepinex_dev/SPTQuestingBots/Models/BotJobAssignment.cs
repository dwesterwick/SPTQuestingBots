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
        Complete,
        Failed
    }

    public class BotJobAssignment
    {
        public JobAssignmentStatus Status { get; private set; } = JobAssignmentStatus.NotStarted;
        public BotOwner BotOwner { get; private set; }
        public Quest QuestAssignment { get; private set; } = null;
        public QuestObjective QuestObjectiveAssignment { get; private set; } = null;
        public QuestObjectiveStep QuestObjectiveStepAssignment { get; private set; } = null;
        public DateTime AssignmentTime { get; private set; } = DateTime.MaxValue;
        public DateTime EndingTime { get; private set; } = DateTime.MaxValue;

        public bool IsActive => Status == JobAssignmentStatus.Active || Status == JobAssignmentStatus.Pending;
        public double TimeSinceAssignment => (DateTime.Now - AssignmentTime).TotalMilliseconds / 1000.0;
        public double TimeSinceJobEnded => (DateTime.Now - EndingTime).TotalMilliseconds / 1000.0;
        public Vector3? Position => QuestObjectiveStepAssignment?.GetPosition() ?? null;

        public BotJobAssignment(BotOwner bot)
        {
            BotOwner = bot;
        }

        public BotJobAssignment(BotOwner bot, Quest quest, QuestObjective objective) : this(bot)
        {
            QuestAssignment = quest;
            QuestObjectiveAssignment = objective;
            TrySetNextObjectiveStep();

            AssignmentTime = DateTime.Now;
        }

        public override string ToString()
        {
            int stepNumber = QuestObjectiveAssignment?.GetObjectiveStepNumber(QuestObjectiveStepAssignment) ?? 0;
            return "Step #" + stepNumber + " for objective " + (QuestObjectiveAssignment?.ToString() ?? "???") + " in quest " + QuestAssignment.Name;
        }

        public bool TrySetNextObjectiveStep()
        {
            QuestObjectiveStepAssignment = QuestObjectiveAssignment.GetNextObjectiveStep(QuestObjectiveStepAssignment);
            if (QuestObjectiveStepAssignment == null)
            {
                return false;
            }

            Status = JobAssignmentStatus.Pending;
            return true;
        }

        public void CompleteJobAssingment()
        {
            endJobAssingment();
            Status = JobAssignmentStatus.Complete;

            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has completed " + ToString());
        }

        public void FailJobAssingment()
        {
            endJobAssingment();
            Status = JobAssignmentStatus.Failed;

            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " has failed " + ToString());
        }

        private void endJobAssingment()
        {
            EndingTime = DateTime.Now;
        }
    }
}
