using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class InteractiveObjectHelpers
    {
        public static Item GenerateKey(this Door door)
        {
            // Create an instance of the class used to generate new ID's for items
            // TO DO: Not sure if this should use false
            MongoID IDGenerator = new MongoID(false);

            // Create a new item for the key needed to unlock the door
            Item keyItem = Singleton<ItemFactory>.Instance.CreateItem(IDGenerator, door.KeyId, null);
            if (keyItem == null)
            {
                LoggingController.LogError("Cannot create key for door " + door.Id);
                return null;
            }

            return keyItem;
        }

        public static InteractionResult GetInteractionResult(this Door door, EInteractionType interactionType, BotOwner botOwner, KeyComponent key = null)
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

                GClass2964 unlockDoorInteractionResult = new GClass2964(key, null, true);
                if (unlockDoorInteractionResult == null)
                {
                    throw new InvalidOperationException(botOwner.GetText() + " cannot use key " + key.Item.LocalizedName() + " to unlock door " + door.Id);
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

                throw;
            }
        }

        public static void UnlockDoor(this BotOwner botOwner, Door door, InteractionResult interactionResult)
        {
            try
            {
                if (door == null)
                {
                    throw new ArgumentNullException(nameof(door));
                }

                // Modified version of BotOwner.DoorOpener.Interact(door, EInteractionType.Unlock) that can use an InteractionResult with a key component

                Type doorOpenerType = typeof(BotDoorOpener);

                botOwner.DoorOpener.Interacting = true;

                float _traversingEnd = Time.time + botOwner.Settings.FileSettings.Move.WAIT_DOOR_OPEN_SEC;

                FieldInfo traversingEndField = doorOpenerType.GetField("_traversingEnd", BindingFlags.NonPublic | BindingFlags.Instance);
                traversingEndField.SetValue(botOwner.DoorOpener, _traversingEnd);

                LoggingController.LogInfo(botOwner.GetText() + " is unlocking door " + door.Id + "...");
                botOwner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, interactionResult, null);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static void ToggleSwitch(this BotOwner botOwner, WorldInteractiveObject sw, EInteractionType interactionType)
        {
            try
            {
                if (sw == null)
                {
                    throw new ArgumentNullException(nameof(sw));
                }

                Player player = botOwner.GetPlayer;
                if (player == null)
                {
                    throw new InvalidOperationException("Cannot get Player object from " + botOwner.GetText());
                }

                player.MovementContext.ExecuteInteraction(sw, new InteractionResult(interactionType));
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }
    }
}
