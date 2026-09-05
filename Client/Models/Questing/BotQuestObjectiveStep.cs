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
using QuestingBots.Helpers;
using QuestingBots.Utils;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public enum QuestAction
    {
        Undefined,
        MoveToPosition,
        Teleport,
        HoldAtPosition,
        Ambush,
        Snipe,
        PlantItem,
        ToggleSwitch,
        RequestExtract,
        CloseNearbyDoors,
        OpenNearbyDoors
    }

    public class BotQuestObjectiveStep
    {
        [JsonProperty("waitTimeAfterCompleting")]
        public double WaitTimeAfterCompleting { get; set; } = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.DefaultWaitTimeAfterObjectiveCompletion;

        [JsonProperty("position")]
        public SerializableVector3? SerializablePosition { get; set; } = null;

        [JsonProperty("lookToPosition")]
        public SerializableVector3? SerializableLookToPosition { get; set; } = null;

        [JsonProperty("targetPosition")]
        public SerializableVector3? SerializableTargetPosition { get; set; } = null;

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
        public float ChanceOfHavingKey { get; set; } = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.UnlockingDoors.DefaultChanceOfBotsHavingKeys;

        [JsonProperty("forceUnlock")]
        public bool ForceUnlock { get; set; } = false;

        [JsonProperty("requireForFollowers")]
        public bool RequireForFollowers { get; set; } = false;

        [JsonIgnore]
        public int? StepNumber { get; set; } = null;

        [JsonIgnore]
        public WorldInteractiveObject? InteractiveObject { get; set; } = null;

        [JsonIgnore]
        private Vector3? _position = null;
        public Vector3? Position
        {
            get
            {
                if (_position == null)
                {
                    if ((SerializablePosition != null) && !SerializablePosition.HasNaNComponent())
                    {
                        _position = SerializablePosition.ToUnityVector3();
                    }
                    else
                    {
                        Singleton<LoggingUtil>.Instance.LogWarning("SerializablePosition is invalid for step: " + SerializablePosition?.ToString() ?? " [NULL]");
                    }
                }

                return _position;
            }
        }

        [JsonIgnore]
        private Vector3? _lookToPosition = null;
        public Vector3? LookToPosition
        {
            get
            {
                if (_lookToPosition == null)
                {
                    if ((SerializableLookToPosition != null) && !SerializableLookToPosition.HasNaNComponent())
                    {
                        _lookToPosition = SerializableLookToPosition.ToUnityVector3();
                    }
                }

                return _lookToPosition;
            }
        }

        [JsonIgnore]
        private Vector3? _targetPosition = null;
        public Vector3? TargetPosition
        {
            get
            {
                if (_targetPosition == null)
                {
                    if ((SerializableTargetPosition != null) && !SerializableTargetPosition.HasNaNComponent())
                    {
                        _targetPosition = SerializableTargetPosition.ToUnityVector3();
                    }
                }

                return _targetPosition;
            }
        }

        public BotQuestObjectiveStep()
        {

        }

        public BotQuestObjectiveStep(SerializableVector3 position) : this()
        {
            SerializablePosition = position;
        }

        public BotQuestObjectiveStep(Vector3 position) : this()
        {
            SerializablePosition = position.ToSerializableVector3();
        }

        public BotQuestObjectiveStep(Vector3 position, QuestAction actionType) : this(position)
        {
            ActionType = actionType;
        }

        public BotQuestObjectiveStep(Vector3 position, QuestAction actionType, Configuration.MinMaxConfig minElapsedTime) : this(position, actionType)
        {
            MinElapsedTime = minElapsedTime;
        }

        public override string ToString()
        {
            return "Step " + (StepNumber.HasValue ? ("#" + StepNumber.Value.ToString()) : "???");
        }

        public bool TrySnapToNavMesh(float maxDistance)
        {
            if (SerializablePosition == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Objective step does not have a position defined for it.");
                return false;
            }

            Vector3? navMeshPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindNearestNavMeshPosition(SerializablePosition.ToUnityVector3(), maxDistance);
            if (!navMeshPosition.HasValue)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot find NavMesh position for " + SerializablePosition.ToUnityVector3().ToString());
                return false;
            }

            _position = null;
            SerializablePosition = navMeshPosition.Value.ToSerializableVector3();
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
