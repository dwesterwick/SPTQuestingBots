using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models
{
    public class QuestObjective
    {
        public Vector3? Position { get; set; } = null;
        public int MaxBots { get; set; } = 2;
        public float MinDistanceFromBot { get; set; } = 10f;
        public float MaxDistanceFromBot { get; set; } = 9999f;

        private List<BotOwner> assignedBots = new List<BotOwner>();
        private List<BotOwner> successfulBots = new List<BotOwner>();
        private List<BotOwner> unsuccessfulBots = new List<BotOwner>();

        public bool CanAssignMoreBots => assignedBots.Count < MaxBots;

        public QuestObjective()
        {

        }

        public override string ToString()
        {
            return "Unnamed Quest Objective";
        }

        public virtual void Clear()
        {
            assignedBots.Clear();
            successfulBots.Clear();
            unsuccessfulBots.Clear();
            Position = null;
            MaxBots = 1;
        }

        public bool TryAssignBot(BotOwner bot)
        {
            if (!CanAssignMoreBots)
            {
                return false;
            }

            if (!assignedBots.Contains(bot))
            {
                assignedBots.Add(bot);
            }

            return true;
        }

        public void RemoveBot(BotOwner bot)
        {
            if (assignedBots.Contains(bot))
            {
                assignedBots.Remove(bot);
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

            if (!Position.HasValue)
            {
                return false;
            }

            float distanceFromObjective = Vector3.Distance(bot.Position, Position.Value);

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
