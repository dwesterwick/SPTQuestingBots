using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using Newtonsoft.Json;
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

        [JsonProperty("steps")]
        private QuestObjectiveStep[] questObjectiveSteps = new QuestObjectiveStep[0];

        [JsonIgnore]
        private Dictionary<BotOwner, byte> currentStepsForBots = new Dictionary<BotOwner, byte>();

        [JsonIgnore]
        private List<BotOwner> successfulBots = new List<BotOwner>();

        [JsonIgnore]
        private List<BotOwner> unsuccessfulBots = new List<BotOwner>();

        public bool CanAssignMoreBots => currentStepsForBots.Count < MaxBots;
        public int StepCount => questObjectiveSteps.Length;
        public ReadOnlyCollection<BotOwner> SuccessfulBots => new ReadOnlyCollection<BotOwner>(successfulBots);
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
            return "Unnamed Quest Objective";
        }

        public virtual void Clear()
        {
            currentStepsForBots.Clear();
            successfulBots.Clear();
            unsuccessfulBots.Clear();

            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.SetPosition(null);
            }
        }

        public void AddStep(QuestObjectiveStep step)
        {
            questObjectiveSteps = questObjectiveSteps.Append(step).ToArray();
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
            if (questObjectiveSteps.Length > 0)
            {
                questObjectiveSteps[0].SetPosition(position);
            }
        }

        public void SetAllPositions(Vector3 position)
        {
            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.SetPosition(position);
            }
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

            if (questObjectiveSteps.Length - 1 > currentStepsForBots[bot])
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
            if (!successfulBots.Contains(bot))
            {
                successfulBots.Add(bot);
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
            if ((!IsRepeatable && successfulBots.Contains(bot)) || unsuccessfulBots.Contains(bot))
            {
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
    }
}
