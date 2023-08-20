using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using QuestingBots.Controllers;
using UnityEngine;

namespace QuestingBots.BotLogic
{
    internal class PMCObjective : MonoBehaviour
    {
        public bool IsObjectiveActive { get; private set; } = false;
        public bool IsObjectiveReached { get; private set; } = false;
        public bool CanChangeObjective { get; set; } = true;
        public bool CanRushPlayerSpawn { get; private set; } = false;
        public bool CanReachObjective { get; private set; } = true;
        public bool IsObjectivePathComplete { get; set; } = true;
        public float MinTimeAtObjective { get; set; } = 10f;
        public Vector3? Position { get; set; } = null;

        private BotOwner botOwner = null;
        private Models.Quest targetQuest = null;
        private Models.QuestObjective targetObjective = null;
        private Stopwatch timeSpentAtObjectiveTimer = new Stopwatch();
        private Stopwatch timeSinceChangingObjectiveTimer = Stopwatch.StartNew();

        public double TimeSpentAtObjective
        {
            get { return timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public double TimeSinceChangingObjective
        {
            get { return timeSinceChangingObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            IsObjectiveActive = botOwner.Side != EPlayerSide.Savage;
            CanRushPlayerSpawn = BotGenerator.IsBotFromInitialPMCSpawns(botOwner);

            ChangeObjective();
        }

        public void CompleteObjective()
        {
            IsObjectivePathComplete = true;
            IsObjectiveReached = true;
            targetObjective.BotCompletedObjective(botOwner);
        }

        public void RejectObjective()
        {
            IsObjectivePathComplete = true;
            CanReachObjective = false;
            targetObjective.BotFailedObjective(botOwner);
        }

        public override string ToString()
        {
            if (targetQuest != null)
            {
                return (targetObjective?.ToString() ?? "???") + " for quest " + targetQuest.Name;
            }

            return "Position " + (Position?.ToString() ?? "???");
        }

        public void ChangeObjective()
        {
            if (!CanChangeObjective)
            {
                return;
            }

            if (TryToGoToRandomQuestObjective())
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has accepted objective " + ToString());
            }
            //LoggingController.LogWarning("Could not assign quest for bot " + botOwner.Profile.Nickname);
        }

        private void Update()
        {
            if (!IsObjectiveActive)
            {
                return;
            }

            if (LocationController.IsABoss(botOwner))
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is a boss. Turning off PMCObjective brain layer.");
                IsObjectiveActive = false;
            }

            if (IsObjectiveReached)
            {
                timeSpentAtObjectiveTimer.Start();
            }
            else
            {
                timeSpentAtObjectiveTimer.Reset();
            }

            if (!Position.HasValue)
            {
                ChangeObjective();
            }
        }

        private bool TryToGoToRandomQuestObjective()
        {
            while (targetQuest == null)
            {
                targetQuest = BotQuestController.GetRandomQuestForBot(botOwner);

                if (targetQuest == null)
                {
                    LoggingController.LogWarning("Could not find a quest for bot " + botOwner.Profile.Nickname);
                    return false;
                }

                if (targetQuest.GetRemainingObjectiveCount(botOwner) == 0)
                {
                    targetQuest.BlacklistBot(botOwner);
                    targetQuest = null;
                }
            }

            Models.QuestObjective nextObjective = targetQuest.GetRandomNewObjective(botOwner);
            if (nextObjective == null)
            {
                LoggingController.LogWarning("Could not find another objective for bot " + botOwner.Profile.Nickname + " for quest " + targetQuest.Name);
                targetQuest.BlacklistBot(botOwner);
                targetQuest = null;
                return false;
            }

            if (!nextObjective.TryAssignBot(botOwner))
            {
                LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot be assigned to " + ToString() + ". Too many bots already assigned to it.");
                return false;
            }

            if (!nextObjective.Position.HasValue)
            {
                LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot be assigned to " + ToString() + targetQuest.Name + ". Invalid position.");
                nextObjective.BotFailedObjective(botOwner);
                return false;
            }

            targetObjective = nextObjective;
            updateObjective(targetObjective.Position.Value);
            
            return true;
        }

        private void updateObjective(Vector3 newTargetPosition)
        {
            Position = newTargetPosition;
            IsObjectiveReached = false;
            CanReachObjective = true;
            timeSinceChangingObjectiveTimer.Restart();
        }
    }
}
