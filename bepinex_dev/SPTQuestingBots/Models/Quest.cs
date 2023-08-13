using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.InventoryLogic;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models
{
    public class Quest
    {
        public RawQuestClass Template { get; private set; } = null;
        public int MinLevel { get; set; } = 0;
        public float ChanceForSelecting { get; set; } = 0.5f;
        public int Priority { get; set; }

        private string name = "Unnamed Quest";
        private List<QuestObjective> objectives = new List<QuestObjective>();
        private List<BotOwner> blacklistedBots = new List<BotOwner>();

        public string Name => Template?.Name ?? name;
        public string TemplateId => Template?.TemplateId ?? "";
        public ReadOnlyCollection<QuestObjective> AllObjectives => new ReadOnlyCollection<QuestObjective>(objectives);
        public IEnumerable<QuestObjective> ValidObjectives => AllObjectives.Where(o => o.Position.HasValue);
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

            foreach(QuestObjective objective in objectives)
            {
                objective.Clear();
            }
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

        public QuestObjective GetObjectiveForZoneID(string zoneId)
        {
            IEnumerable<QuestZoneObjective> matchingObjectives = objectives
                .OfType<QuestZoneObjective>()
                .Where(o => o?.ZoneID == zoneId);
            
            if (matchingObjectives.Count() == 0)
            {
                //LoggingController.LogWarning("Could not find a quest objective for zone " + zoneId);
                return null;
            }
            if (matchingObjectives.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple quest objectives for zone " + zoneId + ": " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + Name + ". Returning the first one.");
            }

            return matchingObjectives.First();
        }

        public QuestObjective GetObjectiveForLootItem(LootItem item)
        {
            IEnumerable<QuestItemObjective> matchingObjectives = objectives
                .OfType<QuestItemObjective>()
                .Where(o => o.Item?.TemplateId == item.TemplateId);
            
            if (matchingObjectives.Count() == 0)
            {
                //LoggingController.LogWarning("Could not find a quest objective for item " + item.Item.LocalizedName());
                return null;
            }
            if (matchingObjectives.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple quest objectives for item " + item.Item.LocalizedName() + ": " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + Name + ". Returning the first one.");
            }

            return matchingObjectives.First();
        }

        public QuestObjective GetObjectiveForLootItem(string templateID)
        {
            IEnumerable<QuestItemObjective> matchingObjectives = objectives
                .OfType<QuestItemObjective>()
                .Where(o => o.Item?.TemplateId == templateID);
            
            if (matchingObjectives.Count() == 0)
            {
                //LoggingController.LogWarning("Could not find a quest objective for item " + templateID);
                return null;
            }
            if (matchingObjectives.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple quest objectives for item " + templateID + ": " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + Name + ". Returning the first one.");
            }

            return matchingObjectives.First();
        }

        public QuestObjective GetObjectiveForSpawnPoint(SpawnPointParams spawnPoint)
        {
            IEnumerable<QuestSpawnPointObjective> matchingObjectives = objectives
                .OfType<QuestSpawnPointObjective>()
                .Where(o => o.SpawnPoint?.Id == spawnPoint.Id);
            
            if (matchingObjectives.Count() == 0)
            {
                //LoggingController.LogWarning("Could not find a quest objective for spawn point " + spawnPoint.ToString());
                return null;
            }
            if (matchingObjectives.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple quest objectives for spawn point " + spawnPoint.ToString() + ": " + string.Join(", ", matchingObjectives.Select(o => o.ToString())) + " for quest " + Name + ". Returning the first one.");
            }

            return matchingObjectives.First();
        }
    }
}
