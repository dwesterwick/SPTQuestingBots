using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.BotLogic.Objective
{
    public class UnlockDoorAction : AbstractWorldInteractiveObjectInteractionAction
    {
        private Vector3? interactionPosition = null!;

        public UnlockDoorAction(BotOwner _BotOwner) : base(_BotOwner, EInteractionType.Unlock, true)
        {
            
        }

        public override void Start()
        {
            base.Start();

            if (!WorldInteractiveObjectExistsForQuestStep())
            {
                ObjectiveManager.FailObjective();
                return;
            }

            if (!GetDoorInteractionPosition())
            {
                ObjectiveManager.FailObjective();
                return;
            }

            if (!DoesWorldInteractiveObjectNeedToBeUnlocked())
            {
                return;
            }

            if (DoesWorldInteractiveObjectNeedToBeBreached() || DoesBotHaveCorrectKey())
            {
                return;
            }

            if (!IsWorldInteractiveObjectAllowedToBeUnlocked())
            {
                Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " cannot unlock " + DesiredWorldInteractiveObject!.Id);
                ObjectiveManager.FailObjective();
                return;
            }

            if (!TryGiveKeyToBot())
            {
                ObjectiveManager.FailObjective();
                return;
            }
        }

        public override void Stop()
        {
            base.Stop();
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

            if (DesiredWorldInteractiveObject == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("WorldInteractiveObject no longer exists");
                ObjectiveManager.FailObjective();

                return;
            }

            if (!interactionPosition.HasValue)
            {
                Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for " + DesiredWorldInteractiveObject.Id);
                ObjectiveManager.FailObjective();

                return;
            }

            ObjectiveManager.StartJobAssigment();

            // Make sure the bot still needs to unlock a door
            if (!ObjectiveManager.MustUnlockDoor)
            {
                return;
            }

            // Check if the door is already unlocked
            if (DesiredWorldInteractiveObject.DoorState == DesiredInteractionType.DesiredDoorState())
            {
                Singleton<LoggingUtil>.Instance.LogWarning(DesiredWorldInteractiveObject.Id + " has already been " + InteractionVerbPastTense);
                InteractionComplete();

                return;
            }

            if (!TryGoToWorldInteractiveObject(interactionPosition!.Value, Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.UnlockingDoors.MaxDistanceToUnlock))
            {
                return;
            }

            if (MustWaitForKeyBundleToLoad())
            {
                return;
            }

            if (!TryExecuteInteraction())
            {
                return;
            }

            InteractionComplete();
        }

        private void InteractionComplete()
        {
            if ((DesiredInteractionType != EInteractionType.Unlock) && (DesiredInteractionType != EInteractionType.Breach))
            {
                return;
            }

            // Report that the door has been unlocked, and wait a few seconds before allowing the bot to recalculate its path to its quest objective.
            // If the questing layer is not paused, there will not be enough time for the NavMesh to update, and the bot will fail its objective. 
            ObjectiveManager.DoorIsUnlocked();
            ObjectiveManager.PauseRequest = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.UnlockingDoors.PauseTimeAfterUnlocking;

            // Assume the door has been unlocked because bots will constantly try breaching some doors otherwise
            Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().ReportUnlockedDoor(DesiredWorldInteractiveObject!);
        }

        private bool GetDoorInteractionPosition()
        {
            if (!(DesiredWorldInteractiveObject is Door))
            {
                Singleton<LoggingUtil>.Instance.LogError(DesiredWorldInteractiveObject!.Id + " is not a Door");
                return false;
            }

            // Determine the location to which the bot should go in order to unlock the door
            interactionPosition = ObjectiveManager.InteractionPositionForDoorToUnlockForObjective;
            if (interactionPosition == null)
            {
                interactionPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetDoorInteractionPosition(DesiredWorldInteractiveObject, BotOwner.Position);
            }
            if (interactionPosition == null)
            {
                Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + DesiredWorldInteractiveObject.Id);
                return false;
            }

            return true;
        }
    }
}
