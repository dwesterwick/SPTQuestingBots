using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public enum QuestAction
    {
        Undefined,
        MoveToPosition,
        HoldAtPosition,
        Ambush,
        Snipe,
        PlantItem,
        ToggleSwitch,
        RequestExtract,
        CloseNearbyDoors
    }

    public class QuestObjectiveStep
    {
        [JsonProperty("waitTimeAfterCompleting")]
        public double WaitTimeAfterCompleting { get; set; } = ConfigController.Config.Questing.DefaultWaitTimeAfterObjectiveCompletion;

        [JsonProperty("position")]
        public SerializableVector3 SerializablePosition { get; set; } = null;

        [JsonProperty("lookToPosition")]
        public SerializableVector3 SerializableLookToPosition { get; set; } = null;

        [JsonProperty("stepType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestAction ActionType { get; set; } = QuestAction.MoveToPosition;

        [JsonProperty("minElapsedTime")]
        public Configuration.MinMaxConfig MinElapsedTime { get; set; } = new Configuration.MinMaxConfig(5, 5);

        [JsonProperty("switchID")]
        public string SwitchID { get; set; } = "";

        [JsonProperty("maxDistance")]
        public float MaxDistance { get; set; } = 5;

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

        public override string ToString()
        {
            return "Step " + (StepNumber.HasValue ? ("#" + StepNumber.Value.ToString()) : "???");
        }

        public Vector3? GetPosition()
        {
            if ((SerializablePosition == null) || SerializablePosition.Any(float.NaN))
            {
                return null;
            }

            return SerializablePosition.ToUnityVector3();
        }

        public Vector3? GetLookToPosition()
        {
            if ((SerializableLookToPosition == null) || SerializableLookToPosition.Any(float.NaN))
            {
                return null;
            }

            return SerializableLookToPosition.ToUnityVector3();
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

            Vector3? navMeshPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindNearestNavMeshPosition(SerializablePosition.ToUnityVector3(), maxDistance);
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

            InteractiveObject = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindSwitch(SwitchID);
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
