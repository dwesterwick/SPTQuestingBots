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
using EFT.InventoryLogic;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Models
{
    public class Quest
    {
        public RawQuestClass Template { get; private set; } = null;
        public int MinLevel { get; set; } = 0;
        public int MaxLevel { get; set; } = 99;
        public float ChanceForSelecting { get; set; } = 50;
        public int Priority { get; set; }

        private string name = "Unnamed Quest";
        private List<QuestObjective> objectives = new List<QuestObjective>();
        private List<BotOwner> blacklistedBots = new List<BotOwner>();

        public string Name => Template?.Name ?? name;
        public string TemplateId => Template?.TemplateId ?? "";
        public ReadOnlyCollection<QuestObjective> AllObjectives => new ReadOnlyCollection<QuestObjective>(objectives);
        public IEnumerable<QuestObjective> ValidObjectives => AllObjectives.Where(o => o.FirstStepPosition != null);
        public int NumberOfObjectives => AllObjectives.Count;
        public int NumberOfValidObjectives => ValidObjectives.Count();

        public Quest(int priority)
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

        public void Clear()
        {
            blacklistedBots.Clear();
            objectives.Clear();
        }

        public void BlacklistBot(BotOwner bot)
        {
            if (!blacklistedBots.Contains(bot))
            {
                blacklistedBots.Add(bot);
            }
        }

        public bool CanAssignBot(BotOwner bot)
        {
            return !blacklistedBots.Contains(bot);
        }

        public void AddObjective(QuestObjective objective)
        {
            objectives.Add(objective);
        }

        public QuestObjective GetRandomObjective()
        {
            IEnumerable<QuestObjective> possibleObjectives = ValidObjectives
                .Where(o => o.CanAssignMoreBots);
            
            if (!possibleObjectives.Any())
            {
                return null;
            }

            return possibleObjectives.Random();
        }

        public QuestObjective GetRandomNewObjective(BotOwner bot)
        {
            IEnumerable<QuestObjective> possibleObjectives = ValidObjectives
                .Where(o => o.CanAssignBot(bot))
                .Where(o => o.CanAssignMoreBots);

            if (!possibleObjectives.Any())
            {
                return null;
            }

            return possibleObjectives.Random();
        }

        public int GetRemainingObjectiveCount(BotOwner bot)
        {
            IEnumerable<QuestObjective> possibleObjectives = ValidObjectives
                .Where(o => o.CanAssignBot(bot))
                .Where(o => o.CanAssignMoreBots);

            return possibleObjectives.Count();
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
    }
}
