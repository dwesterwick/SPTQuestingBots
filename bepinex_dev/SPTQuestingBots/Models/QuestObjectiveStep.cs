using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public enum QuestAction
    {
        Undefined = 0,
        MoveToPosition = 1,
        PlantItem = 2,
    }

    public class QuestObjectiveStep
    {
        [JsonProperty("waitTimeAfterCompleting")]
        public double WaitTimeAfterCompleting { get; set; } = 10;

        [JsonProperty("position")]
        public SerializableVector3 SerializablePosition { get; set; } = null;

        [JsonProperty("stepType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction ActionType { get; set; } = QuestAction.MoveToPosition;

        [JsonProperty("minElapsedTime")]
        public double MinElapsedTime { get; set; } = 0;

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

        public bool TrySnapToNavMesh()
        {
            if (SerializablePosition == null)
            {
                LoggingController.LogError("Objective step does not have a position defined for it.");
                return false;
            }

            Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(SerializablePosition.ToUnityVector3(), ConfigController.Config.QuestGeneration.NavMeshSearchDistanceSpawn);
            if (!navMeshPosition.HasValue)
            {
                LoggingController.LogError("Cannot find NavMesh position for " + SerializablePosition.ToUnityVector3().ToString());
                return false;
            }

            SerializablePosition = new SerializableVector3(navMeshPosition.Value);
            return true;
        }
    }
}
