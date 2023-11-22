using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine.AI;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class UnlockDoorAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private Door door = null;
        private Vector3? interactionPosition = null;

        public UnlockDoorAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {

        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            door = ObjectiveManager.GetCurrentQuestInteractiveObject() as Door;
            if (door == null)
            {
                LoggingController.LogError("Cannot unlock a null door");

                ObjectiveManager.FailObjective();

                return;
            }

            interactionPosition = getInteractionPosition(door);
            if (interactionPosition == null)
            {
                LoggingController.LogError("Cannot find the appropriate interaction position for door " + door.Id);

                ObjectiveManager.FailObjective();

                return;
            }
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            ObjectiveManager.StartJobAssigment();

            if (door.DoorState != EDoorState.Locked)
            {
                LoggingController.LogWarning("Door " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is already unlocked");

                ObjectiveManager.FailObjective();

                return;
            }

            if (checkIfBotIsStuck())
            {
                LoggingController.LogWarning(BotOwner.GetText() + " got stuck while trying to unlock door " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + ". Giving up.");

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return;
            }

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, interactionPosition.Value);
            if (distanceToTargetPosition >= 0.5f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(interactionPosition.Value);

                if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
                {
                    LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to door " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);

                    ObjectiveManager.FailObjective();

                    if (ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }

                return;
            }

            unlockDoor(door, EInteractionType.Unlock);
            ObjectiveManager.DoorIsUnlocked();
            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " unlocked door " + door.Id);

            ObjectiveManager.CompleteObjective();
        }

        private void unlockDoor(Door door, EInteractionType interactionType)
        {
            try
            {
                if (door == null)
                {
                    throw new ArgumentNullException(nameof(door));
                }

                BotOwner.DoorOpener.Interact(door, interactionType);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }
        }

        private Vector3? getInteractionPosition(Door door)
        {
            float searchDistance = 0.75f;
            Vector3[] possibleInteractionPositions = new Vector3[4]
            {
                door.transform.position + new Vector3(searchDistance, 0, 0),
                door.transform.position - new Vector3(searchDistance, 0, 0),
                door.transform.position + new Vector3(0, 0, searchDistance),
                door.transform.position - new Vector3(0, 0, searchDistance)
            };

            foreach (Vector3 possibleInteractionPosition in possibleInteractionPositions)
            {
                Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(possibleInteractionPosition, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    continue;
                }

                LoggingController.LogInfo(BotOwner.GetText() + " is checking the accessibility of position " + navMeshPosition.Value.ToString() + " for door " + door.Id + "...");

                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(BotOwner.Position, navMeshPosition.Value, NavMesh.AllAreas, path);

                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    return navMeshPosition;
                }
            }

            return null;
        }
    }
}
