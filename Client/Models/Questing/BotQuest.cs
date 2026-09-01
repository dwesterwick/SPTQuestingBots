using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using Newtonsoft.Json;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public class BotQuest : JSONObject<BotQuest>
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
        public float MaxTimeOnQuest { get; set; } = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxTimePerQuest;

        [JsonProperty("alarmQuest")]
        public bool AlarmQuest { get; set; } = false;

        [JsonProperty("canRunBetweenObjectives")]
        public bool CanRunBetweenObjectives { get; set; } = true;

        [JsonProperty("requiredSwitches")]
        public Dictionary<string, bool> RequiredSwitches { get; set; } = new Dictionary<string, bool>();

        [JsonProperty("forbiddenWeapons")]
        private WeaponClass[] ForbiddenWeapons { get; set; } = new WeaponClass[0];

        [JsonProperty("botRoleFilter")]
        public WildSpawnType[] BotRoleFilter { get; set; } = new WildSpawnType[0];

        [JsonIgnore]
        public SptRawQuestClass? Template { get; private set; } = null;

        [JsonIgnore]
        public bool IsActiveForPlayer { get; set; } = false;

        [JsonProperty("name")]
        private string name { get; set; } = "Unnamed Quest";

        [JsonProperty("waypoints")]
        private SerializableVector3[] serializableWaypointPositions { get; set; } = new SerializableVector3[0];

        [JsonProperty("objectives")]
        private BotQuestObjective[] objectives { get; set; } = new BotQuestObjective[0];

        [JsonIgnore]
        private IList<Vector3> waypointPositions = null!;

        public bool IsEFTQuest => Template != null;
        
        // Return all objectives in the quest
        public ReadOnlyCollection<BotQuestObjective> AllObjectives => new ReadOnlyCollection<BotQuestObjective>(objectives);
        public int NumberOfObjectives => AllObjectives.Count;

        // Return all objectives in the quest that have valid positions for their first step
        public IEnumerable<BotQuestObjective> ValidObjectives => AllObjectives.Where(o => o.GetFirstStepPosition() != null);
        public int NumberOfValidObjectives => ValidObjectives.Count();

        public BotQuest()
        {

        }

        public BotQuest(string _name) : this()
        {
            name = _name;
        }

        public BotQuest(SptRawQuestClass template) : this()
        {
            Template = template;
        }

        public override string ToString()
        {
            return GetName();
        }

        public string GetName()
        {
            if (Template == null)
            {
                return name;
            }

            if (string.IsNullOrEmpty(Template.Name))
            {
                return Template.SptQuestName;
            }

            return Template.Name;
        }

        public void Clear()
        {
            objectives = new BotQuestObjective[0];
        }

        public IList<Vector3> GetWaypointPositions()
        {
            if (waypointPositions != null)
            {
                return waypointPositions;
            }

            List<Vector3> positions = new List<Vector3>();

            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            float searchDistance = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.QuestGeneration.NavMeshSearchDistanceSpawn;

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
                    Singleton<LoggingUtil>.Instance.LogError("Cannot find NavMesh position for " + uncorrectedPosition.ToString());
                    continue;
                }

                positions.Add(navMeshPosition.Value);
            }

            waypointPositions = positions;
            return positions;
        }

        public bool CanAssignBot(BotOwner bot)
        {
            if (!RaidHelpers.HasRaidStarted())
            {
                return false;
            }

            float raidTime = RaidHelpers.GetRaidElapsedSeconds();

            if (AlarmQuest && !Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().AlarmState)
            {
                return false;
            }

            if (RequiredSwitches.Any(s => !isSwitchInCorrectPosition(s.Key, s.Value)))
            {
                return false;
            }

            if (!bot.HasAnyAllowedWeapon(ForbiddenWeapons))
            {
                return false;
            }

            bool canAssign = canAssignForBotType(bot)
                && ((bot.Profile.Info.Level >= MinLevel) || !Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.ExcludeBotsByLevel)
                && ((bot.Profile.Info.Level <= MaxLevel) || !Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.ExcludeBotsByLevel)
                && (raidTime >= MinRaidET)
                && (raidTime <= MaxRaidET);

            return canAssign;
        }

        public void AddObjective(BotQuestObjective objective)
        {
            objective.UpdateQuestObjectiveStepNumbers();

            objectives = objectives.Append(objective).ToArray();
        }

        public bool TryRemoveObjective(BotQuestObjective objective)
        {
            if (objectives.Length == 0)
            {
                return true;
            }

            int startingLength = objectives.Length;
            objectives = objectives.Where(o => !o.Equals(objective)).ToArray();

            return startingLength == objectives.Length + 1;
        }

        public BotQuestObjective GetObjectiveForZoneID(string zoneId)
        {
            Func<BotQuestZoneObjective, bool> matchTest = o => o?.ZoneID == zoneId;
            return GetObjective(matchTest);
        }

        public BotQuestObjective GetObjectiveForLootItem(LootItem item)
        {
            Func<BotQuestItemObjective, bool> matchTest = o => o.Item?.TemplateId == item.TemplateId;
            return GetObjective(matchTest);
        }

        public BotQuestObjective GetObjectiveForLootItem(string templateID)
        {
            Func<BotQuestItemObjective, bool> matchTest = o => o.Item?.TemplateId == templateID;
            return GetObjective(matchTest);
        }

        public BotQuestObjective GetObjectiveForSpawnPoint(SpawnPointParams spawnPoint)
        {
            Func<BotQuestSpawnPointObjective, bool> matchTest = o => o.SpawnPoint?.Id == spawnPoint.Id;
            return GetObjective(matchTest);
        }

        private BotQuestObjective GetObjective<T>(Func<T, bool> matchTestFunc) where T : BotQuestObjective
        {
            IEnumerable<T> matchingObjectives = objectives
                .OfType<T>()
                .Where(o => matchTestFunc(o) == true);

            if (matchingObjectives.Count() == 0)
            {
                return null!;
            }

            if (matchingObjectives.Count() > 1)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Found multiple quest objectives: " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + GetName() + ". Returning the first one.");
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

        private bool canAssignForBotType(BotOwner bot)
        {
            if (PMCsOnly && !Controllers.BotRegistrationManager.IsBotAPMC(bot))
            {
                return false;
            }

            if ((BotRoleFilter.Length > 0) && !BotRoleFilter.Contains(bot.Profile.Info.Settings.Role))
            {
                return false;
            }

            return true;
        }
    }
}
