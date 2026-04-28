using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.BotLogic.Objective
{
    public abstract class AbstractInteractWithNearbyDoorsAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private EInteractionType desiredInteractionType;
        private bool interactIfLocked;

        private List<Door> nearbyDoors = null!;
        private Door targetDoor = null!;
        private Vector3? interactionPosition = null;
        private bool wasStuck = false;

        public AbstractInteractWithNearbyDoorsAction(BotOwner _BotOwner, EInteractionType _desiredType, bool _interactIfLocked = false) : base(_BotOwner, 100)
        {
            desiredInteractionType = _desiredType;
            interactIfLocked = _interactIfLocked;
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            nearbyDoors = null!;
            targetDoor = null!;
            interactionPosition = null;
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
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " got stuck while going to " + (targetDoor?.Id ?? "[NULL]") + " and will get a new objective.");
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

            // If somebody else is interacting with the door, wait for them to finish. If this takes too long, the "stuck" detection above
            // will catch it
            if ((targetDoor?.DoorState == EDoorState.Interacting) || (targetDoor?.DoorState == EDoorState.Breaching))
            {
                BotOwner.Mover.Stop();
                return;
            }

            // If the current door is already open, select the next one
            if ((targetDoor != null) && (targetDoor.DoorState != desiredInteractionType.OppositeDoorState()))
            {
                tryUpdateTargetDoor();
            }

            // Try to locate where the bot should go to interact with the door
            Vector3 targetPosition = ObjectiveManager.Position.Value;
            if (interactionPosition.HasValue)
            {
                targetPosition = interactionPosition.Value;
            }
            if (!tryGoToPosition(targetPosition, 0.75f))
            {
                return;
            }

            if (targetDoor != null)
            {
                Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " is changing door " + targetDoor.Id + " to " + desiredInteractionType.OppositeDoorState() + "...");

                BotOwner.DoorOpener.Interact(targetDoor, desiredInteractionType);
                nearbyDoors.Remove(targetDoor);

                targetDoor = null!;
                interactionPosition = null;
            }
            else
            {
                tryUpdateTargetDoor();
            }
        }

        private bool tryGoToPosition(Vector3 targetPosition, float maxDistanceFromTargetPosition)
        {
            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, targetPosition);
            if (distanceToTargetPosition < maxDistanceFromTargetPosition)
            {
                return true;
            }

            // If the bot cannot find a complete path to the door, it will be close to open it
            NavMeshPathStatus? pathStatus = RecalculatePath(targetPosition);
            
            if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(targetPosition, 0.5f) != true))
            {
                Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " cannot find a complete path to " + targetPosition.ToString());

                ObjectiveManager.FailObjective();

                if (Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.ShowFailedPaths)
                {
                    drawBotPath(Color.yellow);
                }
            }

            return false;
        }

        private bool tryUpdateTargetDoor()
        {
            targetDoor = tryFindNextOpenDoor();
            interactionPosition = targetDoor.GetDoorInteractionPosition(BotOwner.Position);
            if (interactionPosition == null)
            {
                Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + targetDoor.Id);
                ObjectiveManager.FailObjective();

                return false;
            }

            Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " will interact with door " + targetDoor.Id);

            return true;
        }

        private Door tryFindNextOpenDoor()
        {
            if (ObjectiveManager.MaxDistanceForCurrentStep == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("MaxDistanceForCurrentStep is null");
                return null!;
            }

            if (nearbyDoors == null)
            {
                nearbyDoors = FindNearbyDoors(ObjectiveManager.MaxDistanceForCurrentStep!.Value).ToList();
            }

            IEnumerable<Door> openNearbyDoors = nearbyDoors
                .Where(d => d.DoorState == desiredInteractionType.OppositeDoorState())
                .Where(d => d.DoorState != EDoorState.Locked || interactIfLocked);
            
            if (!openNearbyDoors.Any())
            {
                ObjectiveManager.CompleteObjective();
                return null!;
            }

            return openNearbyDoors.First();
        }
    }
}
