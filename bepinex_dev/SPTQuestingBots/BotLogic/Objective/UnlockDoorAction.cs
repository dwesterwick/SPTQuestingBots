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
        public UnlockDoorAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {

        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();
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

            if (ObjectiveManager.GetCurrentQuestInteractiveObject() == null)
            {
                LoggingController.LogError("Cannot unlock a null door");

                ObjectiveManager.FailObjective();

                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                LoggingController.LogError("Cannot go to a null position");

                ObjectiveManager.FailObjective();

                return;
            }

            ObjectiveManager.StartJobAssigment();

            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState != EDoorState.Locked)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is already unlocked");

                return;
            }

            if (checkIfBotIsStuck())
            {
                LoggingController.LogWarning(BotOwner.GetText() + " got stuck while trying to toggle switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + ". Giving up.");

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return;
            }

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition >= 0.5f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
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

            unlockDoor((Door)ObjectiveManager.GetCurrentQuestInteractiveObject(), EInteractionType.Unlock);
            ObjectiveManager.DoorIsUnlocked();
            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " unlocked door " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);

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
    }
}
