using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;
using SPTQuestingBots.Controllers.Bots;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class UnlockDoorAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private Door door = null;
        private Vector3? interactionPosition = null;
        private InventoryControllerClass inventoryControllerClass = null;
        private IResult keyGenerationResult = null;
        private KeyComponent keyComponent = null;
        private DependencyGraph<IEasyBundle>.GClass3114 bundleLoader = null;

        public UnlockDoorAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            // Ensure a door has been selected for the bot to unlock
            door = ObjectiveManager.GetCurrentQuestInteractiveObject() as Door;
            if (door == null)
            {
                if (ObjectiveManager.MustUnlockDoor)
                {
                    LoggingController.LogError(BotOwner.GetText() + " cannot unlock a null door. InteractiveObject=" + (ObjectiveManager.GetCurrentQuestInteractiveObject()?.Id ?? "???"));

                    ObjectiveManager.FailObjective();
                }

                return;
            }

            // Determine the location to which the bot should go in order to unlock the door
            interactionPosition = LocationController.getDoorInteractionPosition(door, BotOwner.Position);
            if (interactionPosition == null)
            {
                LoggingController.LogError(BotOwner.GetText() + " cannot find the appropriate interaction position for door " + door.Id);

                ObjectiveManager.FailObjective();

                return;
            }

            // Check if the door can be breached and can't be unlocked by a key
            if (door.CanBeBreached && (door.KeyId == ""))
            {
                return;
            }

            // If the door requires a key to open, get the InventoryControllerClass for the bot
            inventoryControllerClass = getInventoryControllerClassForBot(BotOwner);

            // Determine what key is needed to unlock the door, and check if the bot has it
            keyComponent = tryGetKeyComponentForDoor();
            if (keyComponent != null)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " already has key " + keyComponent.Item.LocalizedName() + " for door " + door.Id + "...");
                return;
            }

            // If the bot does not have the key, roll the dice to see if it should be given the key
            System.Random random = new System.Random();
            if (random.Next(1, 100) > ObjectiveManager.ChanceOfHavingKey)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " does not have the key for door " + door.Id + ". Selecting another objective...");

                ObjectiveManager.FailObjective();

                return;
            }

            // If the bot is lucky enough to get the key, try to transfer it to the bot
            if (!tryTransferKeyToBot())
            {
                LoggingController.LogError("Could not transfer key for door " + door.Id + " to " + BotOwner.GetText());

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

            if ((door == null) || !interactionPosition.HasValue)
            {
                return;
            }

            ObjectiveManager.StartJobAssigment();

            // Check if the door is already unlocked
            if (door.DoorState != EDoorState.Locked)
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

            // Make sure the bot still needs to unlock a door
            if (!ObjectiveManager.MustUnlockDoor)
            {
                return;
            }

            // Go to the interaction location selected when the action was created
            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, interactionPosition.Value);
            if (distanceToTargetPosition >= ConfigController.Config.Questing.UnlockingDoors.MaxDistanceToUnlock)
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

            // Check if the door requires a key
            if (door.KeyId != "")
            {
                // Create the key if the bot does not already have it
                if (keyComponent == null)
                {
                    keyComponent = tryGetKeyComponentForDoor();

                    return;
                }

                // Wait for the the bundle to finish loading. If the bundle has not finished loading at this point, something is likely wrong...
                if ((bundleLoader != null) && (!bundleLoader.Finished))
                {
                    LoggingController.LogWarning("Waiting for bundle for " + keyComponent.Item.LocalizedName() + " to load...");

                    return;
                }

                // Load the bundle for the key if it hasn't been already. Otherwise, the unlock animation will fail. 
                if (!isBundleLoaded(keyComponent.Item))
                {
                    LoggingController.LogInfo("Loading bundle for " + keyComponent.Item.LocalizedName() + "...");
                    loadBundle(keyComponent.Item);

                    return;
                }
            }

            // Create the interaction result for the door
            EInteractionType interactionType = EInteractionType.Unlock;
            if (door.CanBeBreached && (door.KeyId == ""))
            {
                interactionType = EInteractionType.Breach;
            }
            InteractionResult interactionResult = getDoorInteractionResult(interactionType, keyComponent);

            // Instruct the bot to unlock the door
            unlockDoor(door, interactionResult);

            // Report that the door has been unlocked, and wait a few seconds before allowing the bot to recalculate its path to its quest objective.
            // If the questing layer is not paused, there will not be enough time for the NavMesh to update, and the bot will fail its objective. 
            ObjectiveManager.DoorIsUnlocked();
            ObjectiveManager.PauseRequest = ConfigController.Config.Questing.UnlockingDoors.PauseTimeAfterUnlocking;
            
            LoggingController.LogInfo("Bot " + BotOwner.GetText() + " unlocked door " + door.Id);
        }

        private static InventoryControllerClass getInventoryControllerClassForBot(BotOwner bot)
        {
            Type playerType = typeof(Player);

            FieldInfo inventoryControllerField = playerType.GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);
            return (InventoryControllerClass)inventoryControllerField.GetValue(bot.GetPlayer);
        }

        private bool tryTransferKeyToBot()
        {
            try
            {
                // Create an instance of the class used to generate new ID's for items
                // TO DO: Not sure if this should use false
                MongoID IDGenerator = new MongoID(false);

                // Create a new item for the key needed to unlock the door
                Item keyItem = Singleton<ItemFactory>.Instance.CreateItem(IDGenerator, door.KeyId, null);
                if (keyItem == null)
                {
                    LoggingController.LogError("Cannot create key for door " + door.Id + " for " + BotOwner.GetText());
                    return false;
                }

                // Enumerate all possible equipment slots into which the key can be transferred
                List<EquipmentSlot> possibleSlots = new List<EquipmentSlot>();
                if (BotRegistrationManager.IsBotAPMC(BotOwner))
                {
                    possibleSlots.Add(EquipmentSlot.SecuredContainer);
                }
                possibleSlots.AddRange(new EquipmentSlot[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.ArmorVest, EquipmentSlot.Pockets });

                // Try to find an available grid in the equipment slots to which the key can be transferred
                ItemAddress locationForItem = findLocationForItem(keyItem, possibleSlots, inventoryControllerClass);
                if (locationForItem == null)
                {
                    LoggingController.LogError("Cannot find any location to put key " + keyItem.LocalizedName() + " for " + BotOwner.GetText());
                    return false;
                }

                // Initialize the transation to transfer the key to the bot
                GStruct375<GClass2593> moveResult = GClass2585.Add(keyItem, locationForItem, inventoryControllerClass, true);
                if (!moveResult.Succeeded)
                {
                    LoggingController.LogError("Cannot move key " + keyItem.LocalizedName() + " to inventory of " + BotOwner.GetText());
                    return false;
                }

                // Execute the transation to transfer the key to the bot
                Callback callback = new Callback(transferredKeyCallback);
                inventoryControllerClass.TryRunNetworkTransaction(moveResult, callback);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }
        }

        private ItemAddress findLocationForItem(Item item, IEnumerable<EquipmentSlot> possibleSlots, InventoryControllerClass botInventoryController)
        {
            foreach (EquipmentSlot slot in possibleSlots)
            {
                //LoggingController.LogInfo("Checking " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                // Search through all grids in the equipment slot
                SearchableItemClass equipmentSlot = botInventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem as SearchableItemClass;
                foreach (GClass2318 grid in (equipmentSlot?.Grids ?? (new GClass2318[0])))
                {
                    //LoggingController.LogInfo("Checking grid " + grid.ID + " (" + grid.GridWidth.Value + "x" + grid.GridHeight.Value + ") in " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                    // Check if the grid has enough free space to fit the item
                    LocationInGrid locationInGrid = grid.FindFreeSpace(item);
                    if (locationInGrid != null)
                    {
                        LoggingController.LogInfo(BotOwner.GetText() + " will receive " + item.LocalizedName() + " in its " + slot.ToString() + "...");

                        return new GClass2580(grid, locationInGrid);
                    }
                }
            }

            return null;
        }

        private void transferredKeyCallback(IResult result)
        {
            keyGenerationResult = result;

            if (result.Succeed)
            {
                LoggingController.LogInfo("Moved key to inventory of " + BotOwner.GetText());
            }

            if (result.Failed)
            {
                LoggingController.LogError("Could not move key to inventory of " + BotOwner.GetText());
            }
        }

        private bool isBundleLoaded(Item item)
        {
            try
            {
                IEasyBundle data = Singleton<IEasyAssets>.Instance.System.GetNode(item.Prefab.path).Data;

                if (data.LoadState.Value == Diz.DependencyManager.ELoadState.Loaded)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }

            return false;
        }

        private void loadBundle(Item item)
        {
            try
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                if (item.Prefab.path.Length == 0)
                {
                    throw new InvalidOperationException("The prefab path for " + item.LocalizedName() + " is empty");
                }

                if (bundleLoader != null)
                {
                    LoggingController.LogInfo("Releasing bundle loader...");
                    bundleLoader.Release();
                }

                bundleLoader = Singleton<IEasyAssets>.Instance.Retain(new string[] { item.Prefab.path }, null, default);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }
        }

        private KeyComponent tryGetKeyComponentForDoor()
        {
            try
            {
                IEnumerable<KeyComponent> matchingKeys = inventoryControllerClass.Inventory.Equipment
                    .GetItemComponentsInChildren<KeyComponent>(false)
                    .Where(k => k.Template.KeyId == door.KeyId);

                if (!matchingKeys.Any())
                {
                    return null;
                }

                return matchingKeys.First();
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }
        }

        private InteractionResult getDoorInteractionResult(EInteractionType interactionType, KeyComponent key)
        {
            try
            {
                if (interactionType != EInteractionType.Unlock)
                {
                    return new InteractionResult(interactionType);
                }

                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                GClass2761 unlockDoorInteractionResult = new GClass2761(key, null, true);
                if (unlockDoorInteractionResult == null)
                {
                    throw new InvalidOperationException(BotOwner.GetText() + " cannot use key " + key.Item.LocalizedName() + " to unlock door " + door.Id);
                }

                // Reduce the remaining usages for the key after the bot unlocks the door
                if ((key.Template.MaximumNumberOfUsage > 0) && (key.NumberOfUsages < key.Template.MaximumNumberOfUsage - 1))
                {
                    key.NumberOfUsages++;
                }

                return unlockDoorInteractionResult;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                ObjectiveManager.TryChangeObjective();

                throw;
            }
        }

        private void unlockDoor(Door door, InteractionResult interactionResult)
        {
            try
            {
                if (door == null)
                {
                    throw new ArgumentNullException(nameof(door));
                }

                // Modified version of BotOwner.DoorOpener.Interact(door, EInteractionType.Unlock) that uses an InteractionResult with a key component

                Type doorOpenerType = typeof(BotDoorOpener);

                PropertyInfo interactingProperty = doorOpenerType.GetProperty("Interacting", BindingFlags.Public | BindingFlags.Instance);
                interactingProperty.SetValue(BotOwner.DoorOpener, true);

                float _traversingEnd = Time.time + BotOwner.Settings.FileSettings.Move.WAIT_DOOR_OPEN_SEC;

                FieldInfo traversingEndField = doorOpenerType.GetField("_traversingEnd", BindingFlags.NonPublic | BindingFlags.Instance);
                traversingEndField.SetValue(BotOwner.DoorOpener, _traversingEnd);

                LoggingController.LogInfo(BotOwner.GetText() + " is unlocking door " + door.Id + "...");
                BotOwner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, interactionResult, null);
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
