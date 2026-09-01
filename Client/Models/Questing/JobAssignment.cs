using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public class JobAssignment : ICloneable
    {
        public BotQuest QuestAssignment { get; protected set; } = null!;
        public BotQuestObjective QuestObjectiveAssignment { get; protected set; } = null!;
        public BotQuestObjectiveStep QuestObjectiveStepAssignment { get; protected set; } = null!;

        public Vector3? Position => QuestObjectiveStepAssignment?.GetPosition();
        public bool IsSpawnSearchQuest => QuestObjectiveAssignment is BotQuestSpawnPointObjective;

        public JobAssignment()
        {

        }

        public JobAssignment(BotQuest _quest, BotQuestObjective _objective, BotQuestObjectiveStep _step) : this()
        {
            QuestAssignment = _quest;
            QuestObjectiveAssignment = _objective;
            QuestObjectiveStepAssignment = _step;
        }

        public override string ToString()
        {
            string stepNumberText = QuestObjectiveStepAssignment?.StepNumber?.ToString() ?? "???";
            return "Step #" + stepNumberText + " for objective " + (QuestObjectiveAssignment?.ToString() ?? "???") + " in quest " + QuestAssignment.GetName();
        }

        public object Clone()
        {
            return new JobAssignment(QuestAssignment, QuestObjectiveAssignment, QuestObjectiveStepAssignment);
        }
    }
}
