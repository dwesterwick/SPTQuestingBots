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
using QuestingBots.Controllers;
using UnityEngine;
using HarmonyLib;
using QuestingBots.Utils;

namespace QuestingBots.Helpers
{
    public static class InteractiveObjectHelpers
    {
        public static Item GenerateKey(this WorldInteractiveObject door)
        {
            // Create a new item for the key needed to unlock the door
            Item keyItem = Singleton<ItemFactoryClass>.Instance.CreateItem(MongoID.Generate(true), door.KeyId, null);
            if (keyItem == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot create key for door " + door.Id);
                return null!;
            }

            return keyItem;
        }

        public static InteractionResult GetInteractionResult(this WorldInteractiveObject worldInteractiveObject, EInteractionType interactionType, BotOwner botOwner, KeyComponent key = null!)
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

                KeyInteractionResultClass unlockInteractionResult = new KeyInteractionResultClass(key, null, true);
                if (unlockInteractionResult == null)
                {
                    throw new InvalidOperationException(botOwner.GetText() + " cannot use key " + key.Item.LocalizedName() + " to unlock WorldInteractiveObject " + worldInteractiveObject.Id);
                }

                // Reduce the remaining usages for the key after the bot unlocks the door
                if ((key.Template.MaximumNumberOfUsage > 0) && (key.NumberOfUsages < key.Template.MaximumNumberOfUsage - 1))
                {
                    key.NumberOfUsages++;
                }

                return unlockInteractionResult;
            }
            catch (Exception e)
            {
                Singleton<LoggingUtil>.Instance.LogError(e.Message);
                Singleton<LoggingUtil>.Instance.LogError(e.StackTrace);

                throw;
            }
        }

        public static void InteractWithWorldInteractiveObject(this BotOwner botOwner, WorldInteractiveObject worldInteractiveObject, InteractionResult interactionResult)
        {
            try
            {
                if (worldInteractiveObject == null)
                {
                    throw new ArgumentNullException(nameof(worldInteractiveObject));
                }

                if (worldInteractiveObject is Door)
                {
                    // Modified version of BotOwner.DoorOpener.Interact(door, EInteractionType.Unlock) that can use an InteractionResult with a key component

                    botOwner.DoorOpener.Interacting = true;
                    botOwner.DoorOpener.TraversingEnd = Time.time + botOwner.Settings.FileSettings.Move.WAIT_DOOR_OPEN_SEC;
                }

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
                Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " is " + interactionTypeText + " " + worldInteractiveObject.GetType().Name + " " + worldInteractiveObject.Id + "...");

                // StartDoorInteraction worked by itself in SPT-AKI 3.7.6, but starting in 3.8.0, doors would "break" without 
                // also running ExecuteDoorInteraction
                worldInteractiveObject.ExecuteInteractionResult(interactionResult, botOwner.GetPlayer);
            }
            catch (Exception e)
            {
                Singleton<LoggingUtil>.Instance.LogError(e.Message);
                Singleton<LoggingUtil>.Instance.LogError(e.StackTrace);

                throw;
            }
        }

        public static void ExecuteInteractionResult(this WorldInteractiveObject worldInteractiveObject, InteractionResult interactionResult, Player player)
        {
            if (worldInteractiveObject is Door)
            {
                interactionResult.RaiseUnlockEvent(CommandStatus.Begin, player);

                // NOTE: This method MUST be used for Fika compatibility
                player.vmethod_0(worldInteractiveObject, interactionResult, () => { interactionResult.RaiseUnlockEvent(CommandStatus.Succeed, player); });
            }

            if (worldInteractiveObject is Switch)
            {
                interactionResult.RaiseUnlockEvent(CommandStatus.Begin, player);
                interactionResult.RaiseUnlockEvent(CommandStatus.Succeed, player);
            }

            // NOTE: This method MUST be used for Fika compatibility
            // NOTE: Ideally, this should be called after a delay. However, this will require a lot of rewriting.
            player.vmethod_1(worldInteractiveObject, interactionResult);
        }

        private static void RaiseUnlockEvent(this InteractionResult interactionResult, CommandStatus command, Player player)
        {
            KeyInteractionResultClass? unlockInteractionResult = interactionResult as KeyInteractionResultClass;
            if (unlockInteractionResult == null)
            {
                return;
            }

            unlockInteractionResult.RaiseEvents(player.InventoryController, command);
        }
    }
}
