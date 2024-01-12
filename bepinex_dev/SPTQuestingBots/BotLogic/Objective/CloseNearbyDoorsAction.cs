using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class CloseNearbyDoorsAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private List<Door> nearbyDoors = null;
        private Door targetDoor = null;
        private Vector3? interactionPosition = null;
        private bool wasStuck = false;

        public CloseNearbyDoorsAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            nearbyDoors = null;
            targetDoor = null;
            interactionPosition = null;
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
                    LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is stuck and will get a new objective.");
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

            if ((targetDoor?.DoorState == EDoorState.Interacting) || (targetDoor?.DoorState == EDoorState.Breaching))
            {
                BotOwner.Mover.Stop();
                return;
            }

            if ((targetDoor != null) && (targetDoor.DoorState != EDoorState.Open))
            {
                tryUpdateTargetDoor();
            }

            Vector3 targetPosition = ObjectiveManager.Position.Value;
            if (interactionPosition.HasValue)
            {
                targetPosition = interactionPosition.Value;
            }

            if (!tryGoToPosition(targetPosition))
            {
                return;
            }

            if (targetDoor != null)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " is closing door " + targetDoor.Id + "...");

                BotOwner.DoorOpener.Interact(targetDoor, EInteractionType.Close);
                nearbyDoors.Remove(targetDoor);

                targetDoor = null;
                interactionPosition = null;
            }
            else
            {
                tryUpdateTargetDoor();
            }
        }

        private bool tryGoToPosition(Vector3 position)
        {
            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, position);
            if (distanceToTargetPosition < 0.75f)
            {
                return true;
            }

            NavMeshPathStatus? pathStatus = RecalculatePath(position);
            if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
            {
                LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to " + position.ToString());

                ObjectiveManager.FailObjective();

                if (ConfigController.Config.Debug.ShowFailedPaths)
                {
                    drawBotPath(Color.yellow);
                }
            }

            return false;
        }

        private bool tryUpdateTargetDoor()
        {
            targetDoor = tryFindNextOpenDoor();
            interactionPosition = getInteractionPosition(targetDoor);
            if (interactionPosition == null)
            {
                return false;
            }

            LoggingController.LogInfo(BotOwner.GetText() + " will close door " + targetDoor.Id);

            return true;
        }

        private Door tryFindNextOpenDoor()
        {
            if (nearbyDoors == null)
            {
                nearbyDoors = FindNearbyDoors(ObjectiveManager.MaxDistanceForCurrentStep.Value).ToList();
                //debugOpenNearbyDoors();
            }

            IEnumerable<Door> openNearbyDoors = nearbyDoors.Where(d => d.DoorState == EDoorState.Open);
            if (!openNearbyDoors.Any())
            {
                ObjectiveManager.CompleteObjective();
                return null;
            }

            return openNearbyDoors.First();
        }

        private void debugOpenNearbyDoors()
        {
            foreach (Door door in nearbyDoors)
            {
                if (door.DoorState == EDoorState.Shut)
                {
                    door.Interact(new InteractionResult(EInteractionType.Open));
                }
            }
        }

        private Vector3? getInteractionPosition(Door door)
        {
            if (door == null)
            {
                return null;
            }

            Vector3 possibleInteractionPosition = door.transform.position;
            Vector3 vectorToBot = (BotOwner.Position - possibleInteractionPosition).normalized;
            possibleInteractionPosition += new Vector3(0, ConfigController.Config.Questing.UnlockingDoors.DoorApproachPositionSearchOffset, 0);
            possibleInteractionPosition += vectorToBot * ConfigController.Config.Questing.UnlockingDoors.DoorApproachPositionSearchRadius;

            // Determine the location to which the bot should go in order to unlock the door
            float searchRadius = ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn;
            Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(possibleInteractionPosition, searchRadius);
            if (navMeshPosition == null)
            {
                LoggingController.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + door.Id);

                ObjectiveManager.FailObjective();

                if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                {
                    DebugHelpers.outlinePosition(possibleInteractionPosition, Color.yellow, searchRadius);
                }
            }
            else
            {
                if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                {
                    DebugHelpers.outlinePosition(navMeshPosition.Value, Color.green);
                }
            }

            return navMeshPosition;
        }
    }
}
