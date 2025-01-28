using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public enum LootAfterCompleting
    {
        Default = 0,
        Force = 1,
        Inhibit = 2,
    }

    public class QuestObjective
    {
        [JsonProperty("repeatable")]
        public bool IsRepeatable { get; set; } = false;

        [JsonProperty("minDistanceFromBot")]
        public float MinDistanceFromBot { get; set; } = 10f;

        [JsonProperty("maxDistanceFromBot")]
        public float MaxDistanceFromBot { get; set; } = 9999f;

        [JsonProperty("maxRunDistance")]
        public float MaxRunDistance { get; set; } = 0f;

        [JsonProperty("lootAfterCompleting")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LootAfterCompleting LootAfterCompletingSetting { get; set; } = LootAfterCompleting.Force;

        [JsonProperty("doorIDToUnlock")]
        public string DoorIDToUnlock = "";

        [JsonProperty("fixedPositionToUnlockDoor")]
        public SerializableVector3 InteractionPositionToUnlockDoor = null;

        [JsonProperty("name")]
        private string name = "Unnamed Quest Objective";

        [JsonProperty("steps")]
        private QuestObjectiveStep[] questObjectiveSteps = new QuestObjectiveStep[0];

        public ReadOnlyCollection<QuestObjectiveStep> AllSteps => new ReadOnlyCollection<QuestObjectiveStep>(questObjectiveSteps);
        public int StepCount => questObjectiveSteps.Length;

        public QuestObjective()
        {

        }

        public QuestObjective(QuestObjectiveStep[] steps) : this()
        {
            questObjectiveSteps = steps;
        }

        public QuestObjective(QuestObjectiveStep step) : this()
        {
            questObjectiveSteps = new QuestObjectiveStep[1] { step };
        }

        public QuestObjective(Vector3 position) : this()
        {
            questObjectiveSteps = new QuestObjectiveStep[1] { new QuestObjectiveStep(position) };
        }

        public override string ToString()
        {
            return name;
        }

        public virtual void Clear()
        {
            // Steps should never be deleted because some of them are generated from EFT's quests
            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.SetPosition(null);
            }
        }

        public void DeleteAllSteps()
        {
            questObjectiveSteps = new QuestObjectiveStep[0];
        }

        public void AddStep(QuestObjectiveStep step)
        {
            // Immediately plant items after reaching objective locations
            if ((step.ActionType == QuestAction.PlantItem) && (questObjectiveSteps.Length > 0))
            {
                questObjectiveSteps.Last().WaitTimeAfterCompleting = ConfigController.Config.Questing.WaitTimeBeforePlanting;
            }

            questObjectiveSteps = questObjectiveSteps.Append(step).ToArray();

            UpdateQuestObjectiveStepNumbers();
        }

        public void SetName(string _name)
        {
            name = _name;
        }

        public Vector3? GetFirstStepPosition()
        {
            if (questObjectiveSteps.Length == 0)
            {
                return null;
            }

            return questObjectiveSteps[0].GetPosition();
        }

        public void SetFirstPosition(Vector3 position)
        {
            if (questObjectiveSteps.Length == 0)
            {
                throw new InvalidOperationException("There are no steps in the objective.");
            }

            questObjectiveSteps[0].SetPosition(position);
        }

        public void SetAllPositions(Vector3 position)
        {
            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.SetPosition(position);
            }
        }

        public void SetFirstWaitTimeAfterCompleting(float time)
        {
            if (questObjectiveSteps.Length == 0)
            {
                throw new InvalidOperationException("There are no steps in the objective.");
            }

            questObjectiveSteps[0].WaitTimeAfterCompleting = time;
        }

        public IEnumerable<Vector3?> GetAllPositions()
        {
            return questObjectiveSteps.Select(step => step.GetPosition());
        }

        public bool TrySnapAllStepPositionsToNavMesh()
        {
            bool allSnapped = true;

            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                float maxNavMeshDistance = ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn;
                if (!step.TrySnapToNavMesh(maxNavMeshDistance))
                {
                    allSnapped = false;
                    LoggingController.LogError("Unable to snap position " + (step.GetPosition()?.ToString() ?? "???") + " to NavMesh for quest objective " + ToString());
                }
            }

            return allSnapped;
        }

        public bool TryFindAllInteractiveObjects()
        {
            bool allFound = true;

            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                if (!step.TryFindSwitch())
                {
                    allFound = false;
                    LoggingController.LogError("Unable to find switch \"" + step.SwitchID + "\" for quest objective " + ToString());
                }
            }

            return allFound;
        }

        public bool CanAssignBot(BotOwner bot)
        {
            if (questObjectiveSteps.Length == 0)
            {
                //LoggingController.LogWarning("Quest objective " + ToString() + " has no steps.");
                return false;
            }

            Vector3? position = questObjectiveSteps[0].GetPosition();
            if (!position.HasValue)
            {
                return false;
            }

            float distanceFromObjective = Vector3.Distance(bot.Position, position.Value);
            if (distanceFromObjective > MaxDistanceFromBot)
            {
                //LoggingController.LogInfo("Bot is too far from " + this.ToString() + ". Distance: " + distanceFromObjective);
                return false;
            }
            if (distanceFromObjective < MinDistanceFromBot)
            {
                //LoggingController.LogInfo("Bot is too close to " + this.ToString() + ". Distance: " + distanceFromObjective);
                return false;
            }

            return true;
        }

        public QuestObjectiveStep GetNextObjectiveStep(QuestObjectiveStep currentStep, bool allowReset = false)
        {
            if (!allowReset && (currentStep == null))
            {
                return null;
            }

            int currentStepNumber = currentStep?.StepNumber ?? 0;
            IEnumerable<QuestObjectiveStep> nextStep = questObjectiveSteps.Where(s => s.StepNumber == currentStepNumber + 1);

            if (nextStep.Any())
            {
                return nextStep.First();
            }

            return null;
        }

        public void UpdateQuestObjectiveStepNumbers()
        {
            for (int i = 0; i < questObjectiveSteps.Length; i++)
            {
                questObjectiveSteps[i].StepNumber = i + 1;
            }
        }
    }
}
