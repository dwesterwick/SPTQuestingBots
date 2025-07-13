using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class UnlockDoorAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private WorldInteractiveObject worldInteractiveObject = null;
        private Vector3? interactionPosition = null;
        private IResult keyGenerationResult = null;
        private KeyComponent keyComponent = null;
        private DependencyGraphClass<IEasyBundle>.GClass3901 bundleLoader = null;

        public UnlockDoorAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            // Ensure a door has been selected for the bot to unlock
            worldInteractiveObject = ObjectiveManager.GetCurrentQuestInteractiveObject();
            if (worldInteractiveObject == null)
            {
                if (ObjectiveManager.MustUnlockDoor)
                {
                    LoggingController.LogError(BotOwner.GetText() + " cannot unlock a null door. InteractiveObject=" + (ObjectiveManager.GetCurrentQuestInteractiveObject()?.Id ?? "???"));

                    ObjectiveManager.FailObjective();
                }

                return;
            }

            // Determine the location to which the bot should go in order to unlock the door
            interactionPosition = ObjectiveManager.InteractionPositionForDoorToUnlockForObjective;
            if (interactionPosition == null)
            {
                interactionPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetDoorInteractionPosition(worldInteractiveObject, BotOwner.Position);
            }
            if (interactionPosition == null)
            {
                LoggingController.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + worldInteractiveObject.Id);

                ObjectiveManager.FailObjective();

                return;
            }

            // Check if the door can be breached and can't be unlocked by a key
            Door door = ObjectiveManager.GetCurrentQuestInteractiveObject() as Door;
            if ((door != null) && door.CanBeBreached && (door.KeyId == ""))
            {
                return;
            }

            // Determine what key is needed to unlock the door, and check if the bot has it
            keyComponent = BotOwner.FindKeyComponent(worldInteractiveObject);
            if (keyComponent != null)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " already has key " + keyComponent.Item.LocalizedName() + " for door " + worldInteractiveObject.Id + "...");
                return;
            }

            // If the bot does not have the key, roll the dice to see if it should be given the key
            System.Random random = new System.Random();
            if (random.Next(1, 100) > ObjectiveManager.ChanceOfHavingKey)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " does not have the key for door " + worldInteractiveObject.Id + ". Selecting another objective...");

                ObjectiveManager.FailObjective();

                return;
            }

            Item keyItem = worldInteractiveObject.GenerateKey();

            if (!keyItem.TryAddToFakeStash(BotOwner, "fake stash for spawning keys"))
            {
                LoggingController.LogError("Could not add key for door " + worldInteractiveObject.Id + " to fake stash for " + BotOwner.GetText());

                ObjectiveManager.FailObjective();

                return;
            }

            // If the bot is lucky enough to get the key, try to transfer it to the bot
            if (!BotOwner.TryTransferItem(keyItem))
            {
                LoggingController.LogError("Could not transfer key for door " + worldInteractiveObject.Id + " to " + BotOwner.GetText());

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

            if (worldInteractiveObject == null)
            {
                LoggingController.LogError("Door no longer exists");

                ObjectiveManager.FailObjective();

                return;
            }

            if (!interactionPosition.HasValue)
            {
                LoggingController.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + worldInteractiveObject.Id);

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
            if (worldInteractiveObject.DoorState != EDoorState.Locked)
            {
                LoggingController.LogWarning("Door " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is already unlocked");

                ObjectiveManager.DoorIsUnlocked();

                return;
            }

            // Check if EFT was unable to generate the key for the bot
            if (keyGenerationResult?.Failed == true)
            {
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

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            // Go to the interaction location selected when the action was created
            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, interactionPosition.Value);
            if (distanceToTargetPosition >= ConfigController.Config.Questing.UnlockingDoors.MaxDistanceToUnlock)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(interactionPosition.Value);

                //if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
                if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(interactionPosition.Value, 0.5f) != true))
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

            // Check if the door requires a key
            if (worldInteractiveObject.KeyId != "")
            {
                // Create the key if the bot does not already have it
                if (keyComponent == null)
                {
                    keyComponent = BotOwner.FindKeyComponent(worldInteractiveObject);

                    return;
                }

                // Wait for the the bundle to finish loading. If the bundle has not finished loading at this point, something is likely wrong...
                if ((bundleLoader != null) && (!bundleLoader.Finished))
                {
                    LoggingController.LogWarning("Waiting for bundle for " + keyComponent.Item.LocalizedName() + " to load...");

                    return;
                }

                // Load the bundle for the key if it hasn't been already. Otherwise, the unlock animation will fail. 
                if (!keyComponent.Item.IsBundleLoaded())
                {
                    if (bundleLoader != null)
                    {
                        LoggingController.LogInfo("Releasing bundle loader...");
                        bundleLoader.Release();
                    }

                    LoggingController.LogInfo("Loading bundle for " + keyComponent.Item.LocalizedName() + "...");
                    bundleLoader = keyComponent.Item.LoadBundle();

                    return;
                }
            }

            Door door = ObjectiveManager.GetCurrentQuestInteractiveObject() as Door;

            // Create the interaction result for the door
            EInteractionType interactionType = EInteractionType.Unlock;
            if ((door != null) && door.CanBeBreached && (door.KeyId == ""))
            {
                interactionType = EInteractionType.Breach;
            }
            InteractionResult interactionResult = worldInteractiveObject.GetInteractionResult(interactionType, BotOwner, keyComponent);

            // Instruct the bot to unlock the door
            BotOwner.InteractWithDoor(worldInteractiveObject, interactionResult);

            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " unlocked door " + worldInteractiveObject.Id);

            // Report that the door has been unlocked, and wait a few seconds before allowing the bot to recalculate its path to its quest objective.
            // If the questing layer is not paused, there will not be enough time for the NavMesh to update, and the bot will fail its objective. 
            ObjectiveManager.DoorIsUnlocked();
            ObjectiveManager.PauseRequest = ConfigController.Config.Questing.UnlockingDoors.PauseTimeAfterUnlocking;
            
            // Assume the door has been unlocked because bots will constantly try breaching some doors otherwise
            Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().ReportUnlockedDoor(worldInteractiveObject);
        }
    }
}
