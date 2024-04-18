using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using Newtonsoft.Json;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class Quest
    {
        [JsonProperty("repeatable")]
        public bool IsRepeatable { get; set; } = false;

        [JsonProperty("pmcsOnly")]
        public bool PMCsOnly { get; set; } = false;

        [JsonProperty("isCamping")]
        public bool IsCamping { get; set; } = false;

        [JsonProperty("isSniping")]
        public bool IsSniping { get; set; } = false;

        [JsonProperty("minLevel")]
        public int MinLevel { get; set; } = 0;

        [JsonProperty("maxLevel")]
        public int MaxLevel { get; set; } = 99;

        [JsonProperty("maxBots")]
        public int MaxBots { get; set; } = 2;

        [JsonProperty("maxBotsInGroup")]
        public int MaxBotsInGroup { get; set; } = 99;

        [JsonProperty("desirability")]
        public float Desirability { get; set; } = 0;

        [JsonProperty("minRaidET")]
        public float MinRaidET { get; set; } = 0;

        [JsonProperty("maxRaidET")]
        public float MaxRaidET { get; set; } = float.MaxValue;

        [JsonProperty("maxTimeOnQuest")]
        public float MaxTimeOnQuest { get; set; } = ConfigController.Config.Questing.BotQuestingRequirements.MaxTimePerQuest;

        [JsonProperty("canRunBetweenObjectives")]
        public bool CanRunBetweenObjectives { get; set; } = true;

        [JsonProperty("requiredSwitches")]
        public Dictionary<string, bool> RequiredSwitches = new Dictionary<string, bool>();

        [JsonIgnore]
        public RawQuestClass Template { get; private set; } = null;

        [JsonProperty("name")]
        private string name = "Unnamed Quest";

        [JsonProperty("waypoints")]
        private SerializableVector3[] serializableWaypointPositions = new SerializableVector3[0];

        [JsonProperty("objectives")]
        private QuestObjective[] objectives = new QuestObjective[0];

        [JsonIgnore]
        private IList<Vector3> waypointPositions = null;

        [JsonIgnore]
        private Dictionary<(Vector3, Vector3), StaticPathData> staticPaths = new Dictionary<(Vector3, Vector3), StaticPathData>();

        public string Name => Template?.Name ?? name;
        public string TemplateId => Template?.TemplateId ?? "";
        public bool IsEFTQuest => Template != null;
        
        // Return all objectives in the quest
        public ReadOnlyCollection<QuestObjective> AllObjectives => new ReadOnlyCollection<QuestObjective>(objectives);
        public int NumberOfObjectives => AllObjectives.Count;

        // Return all objectives in the quest that have valid positions for their first step
        public IEnumerable<QuestObjective> ValidObjectives => AllObjectives.Where(o => o.GetFirstStepPosition() != null);
        public int NumberOfValidObjectives => ValidObjectives.Count();

        public Quest()
        {

        }

        public Quest(string _name) : this()
        {
            name = _name;
        }

        public Quest(RawQuestClass template) : this()
        {
            Template = template;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Clear()
        {
            objectives = new QuestObjective[0];
        }

        public IList<Vector3> GetWaypointPositions()
        {
            if (waypointPositions != null)
            {
                return waypointPositions;
            }

            List<Vector3> positions = new List<Vector3>();

            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            float searchDistance = ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn;

            foreach (SerializableVector3 serializableVector3 in serializableWaypointPositions)
            {
                if ((serializableVector3 == null) || serializableVector3.Any(float.NaN))
                {
                    continue;
                }

                Vector3 uncorrectedPosition = serializableVector3.ToUnityVector3();
                Vector3? navMeshPosition = locationData.FindNearestNavMeshPosition(uncorrectedPosition, searchDistance);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogError("Cannot find NavMesh position for " + uncorrectedPosition.ToString());
                    continue;
                }

                positions.Add(navMeshPosition.Value);
            }

            waypointPositions = positions;
            return positions;
        }

        public void AddStaticPath(Vector3 from, Vector3 to, StaticPathData pathData)
        {
            staticPaths.Add((from, to), pathData);
        }

        public IList<StaticPathData> GetStaticPaths(Vector3 target)
        {
            IList<StaticPathData> paths = new List<StaticPathData>();
            foreach ((Vector3 from, Vector3 to) in staticPaths.Keys)
            {
                if (to != target)
                {
                    continue;
                }

                if (staticPaths[(from, to)].Status != UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                paths.Add(staticPaths[(from, to)]);
            }

            return paths;
        }

        public bool CanAssignBot(BotOwner bot)
        {
            if (!Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return false;
            }

            float raidTime = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();

            if (RequiredSwitches.Any(s => !isSwitchInCorrectPosition(s.Key, s.Value)))
            {
                return false;
            }

            bool canAssign = (!PMCsOnly || Controllers.BotRegistrationManager.IsBotAPMC(bot))
                && ((bot.Profile.Info.Level >= MinLevel) || !ConfigController.Config.Questing.BotQuestingRequirements.ExcludeBotsByLevel)
                && ((bot.Profile.Info.Level <= MaxLevel) || !ConfigController.Config.Questing.BotQuestingRequirements.ExcludeBotsByLevel)
                && (raidTime >= MinRaidET)
                && (raidTime <= MaxRaidET);

            return canAssign;
        }

        public void AddObjective(QuestObjective objective)
        {
            objective.UpdateQuestObjectiveStepNumbers();

            objectives = objectives.Append(objective).ToArray();
        }

        public bool TryRemoveObjective(QuestObjective objective)
        {
            if (objectives.Length == 0)
            {
                return true;
            }

            int startingLength = objectives.Length;
            objectives = objectives.Where(o => !o.Equals(objective)).ToArray();

            return startingLength == objectives.Length + 1;
        }

        public QuestObjective GetObjectiveForZoneID(string zoneId)
        {
            Func<QuestZoneObjective, bool> matchTest = o => o?.ZoneID == zoneId;
            return GetObjective(matchTest);
        }

        public QuestObjective GetObjectiveForLootItem(LootItem item)
        {
            Func<QuestItemObjective, bool> matchTest = o => o.Item?.TemplateId == item.TemplateId;
            return GetObjective(matchTest);
        }

        public QuestObjective GetObjectiveForLootItem(string templateID)
        {
            Func<QuestItemObjective, bool> matchTest = o => o.Item?.TemplateId == templateID;
            return GetObjective(matchTest);
        }

        public QuestObjective GetObjectiveForSpawnPoint(SpawnPointParams spawnPoint)
        {
            Func<QuestSpawnPointObjective, bool> matchTest = o => o.SpawnPoint?.Id == spawnPoint.Id;
            return GetObjective(matchTest);
        }

        private QuestObjective GetObjective<T>(Func<T, bool> matchTestFunc) where T : QuestObjective
        {
            IEnumerable<T> matchingObjectives = objectives
                .OfType<T>()
                .Where(o => matchTestFunc(o) == true);

            if (matchingObjectives.Count() == 0)
            {
                return null;
            }

            if (matchingObjectives.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple quest objectives: " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + Name + ". Returning the first one.");
            }

            return matchingObjectives.First();
        }

        private bool isSwitchInCorrectPosition(string switchID, bool mustBeOpen)
        {
            EFT.Interactive.Switch requiredSwitch = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindSwitch(switchID);
            if (requiredSwitch == null)
            {
                return true;
            }

            if (mustBeOpen)
            {
                return requiredSwitch.DoorState == EDoorState.Open;
            }

            return requiredSwitch.DoorState != EDoorState.Open;
        }
    }
}
