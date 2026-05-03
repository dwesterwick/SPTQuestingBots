using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.BotLogic.Objective
{
    public abstract class AbstractWorldInteractiveObjectInteractionAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private const string FAKE_STASH_NAME = "fake stash for spawning keys";

        protected EInteractionType DesiredInteractionType { get; private set; }
        protected WorldInteractiveObject? DesiredWorldInteractiveObject { get; private set; } = null;
        
        private bool interactIfLocked;
        private System.Random random = new System.Random();
        private KeyComponent? keyComponent = null;
        private DependencyGraphClass<IEasyBundle>.GClass1659? bundleLoader = null;
        private bool wasStuck = false;

        protected string InteractionVerbPastTense
        {
            get
            {
                string pastTenseVerb = DesiredInteractionType.ToString();
                pastTenseVerb += DesiredInteractionType == EInteractionType.Close ? "d" : "ed";

                return pastTenseVerb;
            }
        }

        public AbstractWorldInteractiveObjectInteractionAction(BotOwner _BotOwner, EInteractionType _desiredType, bool _interactIfLocked = false) : base(_BotOwner, 100)
        {
            DesiredInteractionType = _desiredType;
            interactIfLocked = _interactIfLocked;
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

        protected bool WorldInteractiveObjectExistsForQuestStep()
        {
            DesiredWorldInteractiveObject = ObjectiveManager.GetCurrentQuestInteractiveObject();
            if (DesiredWorldInteractiveObject != null)
            {
                return true;
            }

            Singleton<LoggingUtil>.Instance.LogError(BotOwner.GetText() + " cannot interact with a null WorldInteractiveObject");
            return false;
        }

        protected bool DoesWorldInteractiveObjectNeedToBeUnlocked()
        {
            // Check if the door can be breached and can't be unlocked by a key
            Door? door = DesiredWorldInteractiveObject as Door;
            if ((door != null) && (door.DoorState == EDoorState.Locked))
            {
                if (door.KeyId != "")
                {
                    return true;
                }

                if (door.CanBeBreached)
                {
                    return false;
                }

                Singleton<LoggingUtil>.Instance.LogWarning("Door " + door.Id + " is locked but cannot be breached and has no key assigned for unlocking it");
            }

            Switch? sw = DesiredWorldInteractiveObject as Switch;
            if (sw != null)
            {
                return sw.DoorState == EDoorState.Locked;
            }

            return false;
        }

        protected bool DoesWorldInteractiveObjectNeedToBeBreached()
        {
            Door? door = DesiredWorldInteractiveObject as Door;
            if (door == null)
            {
                return false;
            }

            if (door.DoorState != EDoorState.Locked)
            {
                return false;
            }

            if (door.KeyId != "")
            {
                return false;
            }

            return door.CanBeBreached;
        }

        protected bool IsWorldInteractiveObjectAllowedToBeUnlocked()
        {
            if (!interactIfLocked)
            {
                return false;
            }

            if (ObjectiveManager.ForceUnlock)
            {
                return true;
            }

            // Only doors are allowed to be unlocked by chance
            Door? door = DesiredWorldInteractiveObject as Door;
            if (door == null)
            {
                return false;
            }

            if (random.Next(1, 100) > ObjectiveManager.ChanceOfHavingKey)
            {
                Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " does not have the key for " + door.Id + " (Chance=" + ObjectiveManager.ChanceOfHavingKey + "%)");
                return false;
            }

            return true;
        }

        protected bool DoesBotHaveCorrectKey()
        {
            keyComponent = BotOwner.FindKeyComponent(DesiredWorldInteractiveObject);
            if (keyComponent != null)
            {
                Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " has key " + keyComponent.Item.LocalizedName() + " for " + DesiredWorldInteractiveObject!.Id);
                return true;
            }

            return false;
        }

        protected bool TryGiveKeyToBot()
        {
            if (DesiredWorldInteractiveObject == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot give bot a key for a null WorldInteractiveObject");
                return false;
            }

            Item keyItem = DesiredWorldInteractiveObject.GenerateKey();

            if (!keyItem.TryAddToFakeStash(BotOwner, FAKE_STASH_NAME))
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not add key for " + DesiredWorldInteractiveObject.Id + " to fake stash for " + BotOwner.GetText());
                return false;
            }

            if (!BotOwner.TryTransferItem(keyItem))
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not transfer key for " + DesiredWorldInteractiveObject.Id + " to " + BotOwner.GetText());
                return false;
            }

            return true;
        }

        protected bool MustWaitForKeyBundleToLoad()
        {
            if (DesiredWorldInteractiveObject == null)
            {
                return false;
            }

            if ((DesiredWorldInteractiveObject.DoorState != EDoorState.Locked) || (DesiredWorldInteractiveObject.KeyId == ""))
            {
                return false;
            }

            if (keyComponent == null)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " needs to find key " + DesiredWorldInteractiveObject.KeyId);
                DoesBotHaveCorrectKey();

                return true;
            }

            // Wait for the the bundle to finish loading. This should rarely be needed. 
            if ((bundleLoader != null) && (!bundleLoader.Finished))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Waiting for bundle for key " + keyComponent.Item.LocalizedName() + " to load...");
                return true;
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

                return true;
            }

            return false;
        }

        protected bool TryGoToWorldInteractiveObject(Vector3 targetPosition, float maxDistanceFromTargetPosition)
        {
            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    ObjectiveManager.StuckCount++;
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " got stuck while going to " + DesiredWorldInteractiveObject!.Id + " and will get a new objective.");
                }
                wasStuck = true;

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return false;
            }

            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, targetPosition);
            if (distanceToTargetPosition >= maxDistanceFromTargetPosition)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(targetPosition);

                if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(targetPosition, 0.5f) != true))
                {
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " cannot find a complete path to " + DesiredWorldInteractiveObject!.Id);

                    ObjectiveManager.FailObjective();

                    if (Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }

                return false;
            }

            return true;
        }

        protected bool TryExecuteInteraction()
        {
            if (DesiredWorldInteractiveObject!.DoorState == EDoorState.Breaching)
            {
                Singleton<LoggingUtil>.Instance.LogDebug(DesiredWorldInteractiveObject.InteractingPlayer.Id + " is breaching " + DesiredWorldInteractiveObject.Id);
                return false;
            }

            if ((DesiredWorldInteractiveObject.DoorState == EDoorState.Interacting) && (DesiredWorldInteractiveObject.InteractingPlayer.Id == BotOwner.Id))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " is already interacting with " + DesiredWorldInteractiveObject.Id);
                return false;
            }

            if (DesiredWorldInteractiveObject.DoorState == EDoorState.Locked)
            {
                DesiredInteractionType = EInteractionType.Unlock;
            }

            if (DoesWorldInteractiveObjectNeedToBeBreached())
            {
                DesiredInteractionType = EInteractionType.Breach;
            }

            InteractionResult interactionResult = DesiredWorldInteractiveObject.GetInteractionResult(DesiredInteractionType, BotOwner, keyComponent);
            BotOwner.InteractWithWorldInteractiveObject(DesiredWorldInteractiveObject, interactionResult);

            Singleton<LoggingUtil>.Instance.LogInfo("Bot " + BotOwner.GetText() + " " + InteractionVerbPastTense + " " + DesiredWorldInteractiveObject.Id);
            return true;
        }
    }
}
