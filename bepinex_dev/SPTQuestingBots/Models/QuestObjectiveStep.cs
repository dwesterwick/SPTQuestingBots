using Newtonsoft.Json;
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
        [JsonProperty("position")]
        public SerializableVector3 SerializablePosition { get; set; } = null;

        [JsonProperty("step_type")]
        public QuestObjectiveStepType StepType { get; set; } = QuestObjectiveStepType.MoveToPosition;

        public QuestObjectiveStep()
        {

        }

        public QuestObjectiveStep(SerializableVector3 position) : this()
        {
            SerializablePosition = position;
        }

        public QuestObjectiveStep(Vector3 position) : this()
        {
            SerializablePosition = new SerializableVector3(position);
        }

        public Vector3? GetPosition()
        {
            if ((SerializablePosition == null) || SerializablePosition.Any(float.NaN))
            {
                return null;
            }

            return SerializablePosition.ToUnityVector3();
        }

        public void SetPosition(Vector3? position)
        {
            if (!position.HasValue)
            {
                SerializablePosition = null;
                return;
            }

            SerializablePosition = new SerializableVector3(position.Value);
        }
    }
}
