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
        public Quest QuestAssignment { get; private set; } = null;
        public QuestObjective QuestObjectiveAssignment { get; private set; } = null;
        public QuestObjectiveStep QuestObjectiveStepAssignment { get; private set; } = null;

        public BotJobAssignment()
        {

        }
    }
}
