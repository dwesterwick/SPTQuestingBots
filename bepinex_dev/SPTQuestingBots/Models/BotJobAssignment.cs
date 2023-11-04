using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private Quest questAssignment = null;
        private QuestObjective questObjectiveAssignment = null;
        private QuestObjectiveStep questObjectiveStepAssignment = null;

        public BotJobAssignment(BotOwner bot)
        {
            BotOwner = bot;
        }

        public BotJobAssignment(BotOwner bot, Quest quest, QuestObjective objective) : this(bot)
        {
            questAssignment = quest;
            questObjectiveAssignment = objective;
            TrySetNextObjectiveStep(out questObjectiveStepAssignment);
        }

        public bool TrySetNextObjectiveStep(out QuestObjectiveStep step)
        {
            step = questObjectiveAssignment.GetNextObjectiveStep(BotOwner);
            if (step == null)
            {
                return false;
            }

            questObjectiveStepAssignment = step;
            Status = JobAssignmentStatus.Pending;
            return true;
        }
    }
}
