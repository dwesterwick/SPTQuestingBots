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
using HarmonyLib;

namespace SPTQuestingBots.Helpers
{
    public static class InteractiveObjectHelpers
    {
        public static Item GenerateKey(this WorldInteractiveObject door)
        {
            // Create a new item for the key needed to unlock the door
            Item keyItem = Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(true), door.KeyId, null);
            if (keyItem == null)
            {
                LoggingController.LogError("Cannot create key for door " + door.Id);
                return null;
            }

            return keyItem;
        }

        public static InteractionResult GetInteractionResult(this WorldInteractiveObject door, EInteractionType interactionType, BotOwner botOwner, KeyComponent key = null)
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

                GClass3424 unlockDoorInteractionResult = new GClass3424(key, null, true);
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

        public static void InteractWithDoor(this BotOwner botOwner, WorldInteractiveObject door, InteractionResult interactionResult)
        {
            try
            {
                if (door == null)
                {
                    throw new ArgumentNullException(nameof(door));
                }

                // Modified version of BotOwner.DoorOpener.Interact(door, EInteractionType.Unlock) that can use an InteractionResult with a key component

                botOwner.DoorOpener.Interacting = true;
                botOwner.DoorOpener._traversingEnd = Time.time + botOwner.Settings.FileSettings.Move.WAIT_DOOR_OPEN_SEC;

                string interactionTypeText = "opening";
                switch (interactionResult.InteractionType)
                {
                    case EInteractionType.Unlock:
                        interactionTypeText = "unlocking";
                        break;
                    case EInteractionType.Close:
                        interactionTypeText = "closing";
                        break;
                    case EInteractionType.Breach:
                        interactionTypeText = "breaching";
                        break;
                }
                LoggingController.LogInfo(botOwner.GetText() + " is " + interactionTypeText + " door " + door.Id + "...");

                // StartDoorInteraction worked by itself in SPT-AKI 3.7.6, but starting in 3.8.0, doors would "break" without 
                // also running ExecuteDoorInteraction
                door.ExecuteInteractionResult(interactionResult, botOwner.GetPlayer);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static void ExecuteInteractionResult(this WorldInteractiveObject worldInteractiveObject, InteractionResult interactionResult, Player player)
        {
            if (worldInteractiveObject is Door)
            {
                // NOTE: This method MUST be used for Fika compatibility
                player.vmethod_0(worldInteractiveObject, interactionResult, null);
            }

            // NOTE: This method MUST be used for Fika compatibility
            // NOTE: Ideally, this should be called after a delay. However, this will require a lot of rewriting.
            player.vmethod_1(worldInteractiveObject, interactionResult);
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

                sw.ExecuteInteractionResult(new InteractionResult(interactionType), player);
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
