using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.BotLogic.Objective
{
    public class ToggleSwitchAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private WorldInteractiveObject worldInteractiveObject = null!;
        private KeyComponent keyComponent = null!;
        private DependencyGraphClass<IEasyBundle>.GClass1659 bundleLoader = null!;

        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            worldInteractiveObject = ObjectiveManager.GetCurrentQuestInteractiveObject();
            if (worldInteractiveObject == null)
            {
                Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot interact with a null switch. InteractiveObject=" + (ObjectiveManager.GetCurrentQuestInteractiveObject()?.Id ?? "???"));

                ObjectiveManager.FailObjective();
                return;
            }

            if (ObjectiveManager.ForceUnlock && (worldInteractiveObject.DoorState == EDoorState.Locked))
            {
                // Determine what key is needed to unlock the door, and check if the bot has it
                keyComponent = BotOwner.FindKeyComponent(worldInteractiveObject);
                if (keyComponent != null)
                {
                    Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " already has key " + keyComponent.Item.LocalizedName() + " for switch " + worldInteractiveObject.Id + "...");
                    return;
                }

                Item keyItem = worldInteractiveObject.GenerateKey();

                if (!keyItem.TryAddToFakeStash(BotOwner, "fake stash for spawning keys"))
                {
                    Singleton<LoggingUtil>.Instance.LogError("Could not add key for switch " + worldInteractiveObject.Id + " to fake stash for " + BotOwner.GetText());

                    ObjectiveManager.FailObjective();

                    return;
                }

                // If the bot is lucky enough to get the key, try to transfer it to the bot
                if (!BotOwner.TryTransferItem(keyItem))
                {
                    Singleton<LoggingUtil>.Instance.LogError("Could not transfer key for switch " + worldInteractiveObject.Id + " to " + BotOwner.GetText());

                    ObjectiveManager.FailObjective();

                    return;
                }
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

            if (worldInteractiveObject == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot toggle a null switch");

                ObjectiveManager.FailObjective();

                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot go to a null position");

                ObjectiveManager.FailObjective();

                return;
            }

            ObjectiveManager.StartJobAssigment();

            // If the switch has already been toggled, there is nothing else for the bot to do
            if (worldInteractiveObject.DoorState == EDoorState.Open)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Switch " + worldInteractiveObject.Id + " is already open");

                ObjectiveManager.CompleteObjective();

                return;
            }

            // If players are unable to toggle the switch, the bot shouldn't be allowed either
            if (!ObjectiveManager.ForceUnlock && (worldInteractiveObject.DoorState == EDoorState.Locked))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Switch " + worldInteractiveObject.Id + " is unavailable");

                ObjectiveManager.TryChangeObjective();

                return;
            }

            if (checkIfBotIsStuck())
            {
                Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " got stuck while trying to toggle switch " + worldInteractiveObject.Id + ". Giving up.");

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return;
            }

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition > 0.75f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(ObjectiveManager.Position.Value, 0.5f) != true))
                {
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " cannot find a complete path to switch " + worldInteractiveObject.Id);

                    ObjectiveManager.FailObjective();

                    if (Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }

                return;
            }

            // Check if the switch requires a key
            if ((worldInteractiveObject.DoorState == EDoorState.Locked) && (worldInteractiveObject.KeyId != ""))
            {
                // Create the key if the bot does not already have it
                if (keyComponent == null)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " needs to identify key " + worldInteractiveObject.KeyId);

                    keyComponent = BotOwner.FindKeyComponent(worldInteractiveObject);

                    return;
                }

                // Wait for the the bundle to finish loading. If the bundle has not finished loading at this point, something is likely wrong...
                if ((bundleLoader != null) && (!bundleLoader.Finished))
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Waiting for bundle for " + keyComponent.Item.LocalizedName() + " to load...");

                    return;
                }

                // Load the bundle for the key if it hasn't been already. Otherwise, the unlock animation will fail. 
                if (!keyComponent.Item.IsBundleLoaded())
                {
                    if (bundleLoader != null)
                    {
                        Singleton<LoggingUtil>.Instance.LogInfo("Releasing bundle loader...");
                        bundleLoader.Release();
                    }

                    Singleton<LoggingUtil>.Instance.LogInfo("Loading bundle for " + keyComponent.Item.LocalizedName() + "...");
                    bundleLoader = keyComponent.Item.LoadBundle();

                    return;
                }
            }

            if ((worldInteractiveObject.DoorState == EDoorState.Interacting) && (worldInteractiveObject.InteractingPlayer.Id == BotOwner.Id))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " is already interacting with switch " + worldInteractiveObject.Id);
                return;
            }

            if (ObjectiveManager.ForceUnlock && (worldInteractiveObject.DoorState == EDoorState.Locked))
            {
                InteractionResult interactionResult = worldInteractiveObject.GetInteractionResult(EInteractionType.Unlock, BotOwner, keyComponent);
                BotOwner.InteractWithWorldInteractiveObject(worldInteractiveObject, interactionResult);

                // Switches in Labyrinth only unlock, so the bot shouldn't also try opening them. If they do, the switch gets stuck in EDoorState.Interacting
                ObjectiveManager.CompleteObjective();
                return;
            }

            if (worldInteractiveObject.DoorState == EDoorState.Shut)
            {
                InteractionResult interactionResult = worldInteractiveObject.GetInteractionResult(EInteractionType.Open, BotOwner);
                BotOwner.InteractWithWorldInteractiveObject(worldInteractiveObject, interactionResult);

                ObjectiveManager.CompleteObjective();
                return;
            }

            Singleton<LoggingUtil>.Instance.LogWarning(worldInteractiveObject.InteractingPlayer.GetText() + " is already interacting with switch " + worldInteractiveObject.Id);
            ObjectiveManager.CompleteObjective();
        }
    }
}
