using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using Newtonsoft.Json;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class QuestObjective
    {
        [JsonProperty("repeatable")]
        public bool IsRepeatable { get; set; } = false;

        [JsonProperty("maxBots")]
        public int MaxBots { get; set; } = 2;

        [JsonProperty("minDistanceFromBot")]
        public float MinDistanceFromBot { get; set; } = 10f;

        [JsonProperty("maxDistanceFromBot")]
        public float MaxDistanceFromBot { get; set; } = 9999f;

        [JsonProperty("maxRunDistance")]
        public float MaxRunDistance { get; set; } = 0f;

        [JsonProperty("name")]
        private string name = "Unnamed Quest Objective";

        [JsonProperty("steps")]
        private QuestObjectiveStep[] questObjectiveSteps = new QuestObjectiveStep[0];

        // A dictionary containing all bots currently doing performing this objective and the step number each of them are on
        [JsonIgnore]
        private Dictionary<BotOwner, byte> currentStepsForBots = new Dictionary<BotOwner, byte>();

        // A dictionary containing all bots who have completed this objective and the time when they did
        [JsonIgnore]
        private Dictionary<BotOwner, DateTime> successfulBots = new Dictionary<BotOwner, DateTime>();

        [JsonIgnore]
        private List<BotOwner> unsuccessfulBots = new List<BotOwner>();

        public bool CanAssignMoreBots => currentStepsForBots.Count < MaxBots;
        public int StepCount => questObjectiveSteps.Length;

        public ReadOnlyCollection<BotOwner> SuccessfulBots => new ReadOnlyCollection<BotOwner>(successfulBots.Keys.ToArray());
        public ReadOnlyCollection<BotOwner> UnsuccessfulBots => new ReadOnlyCollection<BotOwner>(unsuccessfulBots);
        public ReadOnlyCollection<BotOwner> ActiveBots => new ReadOnlyCollection<BotOwner>(currentStepsForBots.Keys.ToArray());

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
            currentStepsForBots.Clear();
            successfulBots.Clear();
            unsuccessfulBots.Clear();

            // Steps should never be deleted because some of them are generated from EFT's quests
            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.SetPosition(null);
            }
        }

        public void AddStep(QuestObjectiveStep step)
        {
            questObjectiveSteps = questObjectiveSteps.Append(step).ToArray();
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

        public bool TrySnapAllStepPositionsToNavMesh()
        {
            bool allSnapped = true;

            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                if (!step.TrySnapToNavMesh())
                {
                    allSnapped = false;
                    LoggingController.LogError("Unable to snap position " + (step.GetPosition()?.ToString() ?? "???") + " to NavMesh for quest objective " + ToString());
                }
            }

            return allSnapped;
        }

        public bool TryAssignBot(BotOwner bot)
        {
            if (!CanAssignMoreBots)
            {
                return false;
            }

            if (!currentStepsForBots.ContainsKey(bot))
            {
                currentStepsForBots.Add(bot, 0);
            }

            return true;
        }

        public QuestObjectiveStep GetNextObjectiveStep(BotOwner bot)
        {
            if (!currentStepsForBots.ContainsKey(bot))
            {
                return null;
            }

            // Check if there are any steps left for the bot to perform
            if (currentStepsForBots[bot] > questObjectiveSteps.Length - 1)
            {
                RemoveBot(bot);
                return null;
            }

            return questObjectiveSteps[currentStepsForBots[bot]++];
        }

        public void RemoveBot(BotOwner bot)
        {
            if (currentStepsForBots.ContainsKey(bot))
            {
                currentStepsForBots.Remove(bot);
            }
        }

        public void BotCompletedObjective(BotOwner bot)
        {
            if (!successfulBots.ContainsKey(bot))
            {
                successfulBots.Add(bot, DateTime.Now);
            }
            else
            {
                successfulBots[bot] = DateTime.Now;
            }

            RemoveBot(bot);
        }

        public void BotFailedObjective(BotOwner bot)
        {
            if (!unsuccessfulBots.Contains(bot))
            {
                unsuccessfulBots.Add(bot);
            }

            RemoveBot(bot);
        }

        public bool CanAssignBot(BotOwner bot)
        {
            if (unsuccessfulBots.Contains(bot))
            {
                return false;
            }

            if (successfulBots.ContainsKey(bot))
            {
                if (!IsRepeatable)
                {
                    return false;
                }

                // Don't allow a bot to perform a repeatable objective if it already recently completed it
                TimeSpan timeSinceCompleted = DateTime.Now - successfulBots[bot];
                if (timeSinceCompleted.TotalMilliseconds < ConfigController.Config.BotQuestingRequirements.RepeatQuestDelay)
                {
                    return false;
                }
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
    }
}
