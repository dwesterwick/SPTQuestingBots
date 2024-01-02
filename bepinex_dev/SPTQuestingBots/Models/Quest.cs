using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using Newtonsoft.Json;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots;

namespace SPTQuestingBots.Models
{
    public class Quest
    {
        [JsonProperty("repeatable")]
        public bool IsRepeatable { get; set; } = false;

        [JsonProperty("pmcsOnly")]
        public bool PMCsOnly { get; set; } = false;

        [JsonProperty("minLevel")]
        public int MinLevel { get; set; } = 0;

        [JsonProperty("maxLevel")]
        public int MaxLevel { get; set; } = 99;

        [JsonProperty("maxBots")]
        public int MaxBots { get; set; } = 2;

        [JsonProperty("chanceForSelecting")]
        public float ChanceForSelecting { get; set; } = 50;

        [JsonProperty("priority")]
        public int Priority { get; set; } = 99;

        [JsonProperty("desirability")]
        public float Desirability { get; set; } = -1;

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

        [JsonProperty("objectives")]
        private QuestObjective[] objectives = new QuestObjective[0];

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

        public Quest(int priority) : this()
        {
            Priority = priority;
        }

        public Quest(int priority, string _name): this(priority)
        {
            name = _name;
        }

        public Quest(int priority, RawQuestClass template) : this(priority)
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

            bool canAssign = (!PMCsOnly || BotRegistrationManager.IsBotAPMC(bot))
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
            EFT.Interactive.Switch requiredSwitch = LocationController.FindSwitch(switchID);
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
