using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
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
        HoldAtPosition = 2,
        Ambush = 3,
        PlantItem = 4,
        ToggleSwitch = 5,
        RequestExtract = 6,
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
        public Configuration.MinMaxConfig MinElapsedTime { get; set; } = new Configuration.MinMaxConfig(5, 5);

        [JsonProperty("switchID")]
        public string SwitchID { get; set; } = "";

        [JsonProperty("maxWanderDistance")]
        public float MaxWanderDistance { get; set; } = 5;

        [JsonProperty("chanceOfHavingKey")]
        public float ChanceOfHavingKey { get; set; } = ConfigController.Config.Questing.UnlockingDoors.DefaultChanceOfBotsHavingKeys;

        [JsonIgnore]
        public int? StepNumber { get; set; } = null;

        [JsonIgnore]
        public WorldInteractiveObject InteractiveObject { get; set; } = null;

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

        public QuestObjectiveStep(Vector3 position, QuestAction actionType) : this(position)
        {
            ActionType = actionType;
        }

        public QuestObjectiveStep(Vector3 position, QuestAction actionType, Configuration.MinMaxConfig minElapsedTime) : this(position, actionType)
        {
            MinElapsedTime = minElapsedTime;
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

        public bool TrySnapToNavMesh(float maxDistance)
        {
            if (SerializablePosition == null)
            {
                LoggingController.LogError("Objective step does not have a position defined for it.");
                return false;
            }

            Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(SerializablePosition.ToUnityVector3(), maxDistance);
            if (!navMeshPosition.HasValue)
            {
                LoggingController.LogError("Cannot find NavMesh position for " + SerializablePosition.ToUnityVector3().ToString());
                return false;
            }

            SerializablePosition = new SerializableVector3(navMeshPosition.Value);
            return true;
        }

        public bool TryFindSwitch()
        {
            if (ActionType != QuestAction.ToggleSwitch)
            {
                return true;
            }

            InteractiveObject = LocationController.FindSwitch(SwitchID);
            return InteractiveObject != null;
        }

        public double GetRandomMinElapsedTime()
        {
            System.Random random = new System.Random();
            double selectedTime = MinElapsedTime.Min + ((MinElapsedTime.Max - MinElapsedTime.Min) * random.NextDouble());
            return Math.Round(selectedTime, 1);
        }
    }
}
