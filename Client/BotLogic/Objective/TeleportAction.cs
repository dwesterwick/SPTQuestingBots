using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.BotLogic.Objective
{
    public class TeleportAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private Vector3? teleportTargetPosition = null!;
        private bool wasStuck = false;

        public TeleportAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {

        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            teleportTargetPosition = ObjectiveManager.TargetPosition;
            if (teleportTargetPosition != null)
            {
                teleportTargetPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindNearestNavMeshPosition(teleportTargetPosition.Value, 0.5f);
            }
            if (teleportTargetPosition == null)
            {
                Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot teleport to " + teleportTargetPosition);

                ObjectiveManager.FailObjective();

                return;
            }
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();
        }

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();
            UpdateBotMiscActions();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (!ObjectiveManager.IsJobAssignmentActive)
            {
                return;
            }

            if (!teleportTargetPosition.HasValue)
            {
                return;
            }

            if (!ObjectiveManager.Position.HasValue || !ObjectiveManager.MaxDistanceForCurrentStep.HasValue)
            {
                throw new InvalidOperationException("Cannot go to a null position");
            }

            ObjectiveManager.StartJobAssigment();

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    ObjectiveManager.StuckCount++;
                    Singleton<LoggingUtil>.Instance.LogWarning("Bot " + BotOwner.GetText() + " is stuck and will get a new objective.");
                }
                wasStuck = true;

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }
            }
            else
            {
                wasStuck = false;
            }

            Vector3 targetPosition = ObjectiveManager.Position.Value;
            if (!tryGoToPosition(targetPosition))
            {
                return;
            }

            BotOwner.Mover.Teleport(teleportTargetPosition.Value);
            ObjectiveManager.CompleteObjective();
        }

        private bool tryGoToPosition(Vector3 position)
        {
            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, position);
            if (distanceToTargetPosition < 0.75f)
            {
                return true;
            }

            // If the bot cannot find a complete path to the door, it will be close to open it
            NavMeshPathStatus? pathStatus = RecalculatePath(position);
            //if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
            if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(position, 0.5f) != true))
            {
                Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " cannot find a complete path to " + position.ToString());

                ObjectiveManager.FailObjective();

                if (Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.ShowFailedPaths)
                {
                    drawBotPath(Color.yellow);
                }
            }

            return false;
        }
    }
}
