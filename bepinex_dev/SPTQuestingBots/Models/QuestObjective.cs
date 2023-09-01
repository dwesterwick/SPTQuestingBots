using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class QuestObjective
    {
        public int MaxBots { get; set; } = 2;
        public float MinDistanceFromBot { get; set; } = 10f;
        public float MaxDistanceFromBot { get; set; } = 9999f;

        private QuestObjectiveStep[] questObjectiveSteps = new QuestObjectiveStep[0];
        private Dictionary<BotOwner, byte> currentStepsForBots = new Dictionary<BotOwner, byte>();
        private List<BotOwner> successfulBots = new List<BotOwner>();
        private List<BotOwner> unsuccessfulBots = new List<BotOwner>();

        public bool CanAssignMoreBots => currentStepsForBots.Count < MaxBots;
        public Vector3? FirstStepPosition => questObjectiveSteps.Length > 0 ? questObjectiveSteps[0].Position : null;

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
                step.Position = null;
            }
        }

        public void SetFirstPosition(Vector3 position)
        {
            if (questObjectiveSteps.Length > 0)
            {
                questObjectiveSteps[0].Position = position;
            }
        }

        public void SetAllPositions(Vector3 position)
        {
            foreach (QuestObjectiveStep step in questObjectiveSteps)
            {
                step.Position = position;
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
            if (successfulBots.Contains(bot) || unsuccessfulBots.Contains(bot))
            {
                return false;
            }

            if (!questObjectiveSteps[0].Position.HasValue)
            {
                return false;
            }

            float distanceFromObjective = Vector3.Distance(bot.Position, questObjectiveSteps[0].Position.Value);

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
