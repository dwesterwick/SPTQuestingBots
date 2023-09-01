using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public enum QuestObjectiveStepType
    {
        Undefined = 0,
        MoveToPosition = 1,
    }

    public class QuestObjectiveStep
    {
        public Vector3? Position { get; set; } = null;
        public QuestObjectiveStepType StepType { get; set; } = QuestObjectiveStepType.MoveToPosition;

        public QuestObjectiveStep()
        {

        }

        public QuestObjectiveStep(Vector3 position) : this()
        {
            Position = position;
        }
    }
}
