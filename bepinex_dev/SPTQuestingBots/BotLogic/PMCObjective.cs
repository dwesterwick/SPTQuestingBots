using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
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
        private Stopwatch checkForLootTimer = Stopwatch.StartNew();

        public double TimeSpentAtObjective
        {
            get { return timeSpentAtObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public double TimeSinceChangingObjective
        {
            get { return timeSinceChangingObjectiveTimer.ElapsedMilliseconds / 1000.0; }
        }

        public bool ShouldCheckForLoot
        {
            get { return checkForLootTimer.ElapsedMilliseconds < 3000; }
        }

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            CanRushPlayerSpawn = BotGenerator.IsBotFromInitialPMCSpawns(botOwner);

            if (BotQuestController.IsBotAPMC(botOwner))
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is a PMC. Enabling PMCObjective brain layer.");
                IsObjectiveActive = true;
            }
            else
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is not a PMC. Disabling PMCObjective brain layer.");
                IsObjectiveActive = false;
            }

            if (IsObjectiveActive)
            {
                LoggingController.LogInfo("Setting objective for " + botOwner.Profile.Nickname + " (Brain type: " + botOwner.Brain.BaseBrain.ShortName() + ")");
                TryChangeObjective();
            }
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

        public bool TryChangeObjective()
        {
            if (!CanChangeObjective)
            {
                return false;
            }

            LoggingController.LogInfo("Bot brain layers: " + string.Join(", ", BotLogic.BotBrains.GetBrainLayersForBot(botOwner)));

            if (TryToGoToRandomQuestObjective())
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has accepted objective " + ToString());
                return true;
            }

            //LoggingController.LogWarning("Could not assign quest for bot " + botOwner.Profile.Nickname);
            return false;
        }

        public bool TryCheckForLoot()
        {
            

            /*if (checkForLootTimer.ElapsedMilliseconds > 10000)
            {
                checkForLootTimer.Restart();

                if (LocationController.GetDistanceToNearestLootableContainer(botOwner) > 10)
                {
                    return false;
                }

                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " can check for loot.");
                return true;
            }*/

            return false;
        }

        private void Update()
        {
            if (!IsObjectiveActive)
            {
                return;
            }

            /*if (ConfigController.Config.InitialPMCSpawns.Enabled && LocationController.IsABoss(botOwner))
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is a boss. Turning off PMCObjective brain layer.");
                IsObjectiveActive = false;
            }*/

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
                TryChangeObjective();
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
