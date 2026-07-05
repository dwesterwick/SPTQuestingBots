using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.Objective
{
    public class ToggleSwitchAction : AbstractWorldInteractiveObjectInteractionAction
    {
        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, EInteractionType.Open, true)
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

            if (!DoesWorldInteractiveObjectNeedToBeUnlocked())
            {
                return;
            }

            if (DoesBotHaveCorrectKey())
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

            if (!ObjectiveManager.Position.HasValue)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot go to a null position");
                ObjectiveManager.FailObjective();

                return;
            }

            ObjectiveManager.StartJobAssigment();

            // If the switch has already been toggled, there is nothing else for the bot to do
            if (DesiredWorldInteractiveObject.DoorState == DesiredInteractionType.DesiredDoorState())
            {
                Singleton<LoggingUtil>.Instance.LogWarning(DesiredWorldInteractiveObject.Id + " has already been " + InteractionVerbPastTense);
                ObjectiveManager.CompleteObjective();

                return;
            }

            if (!TryGoToWorldInteractiveObject(ObjectiveManager.Position.Value, 0.75f))
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

            // Switches in Labyrinth only unlock, so the bot shouldn't also try opening them. If they do, the switch gets stuck in EDoorState.Interacting
            ObjectiveManager.CompleteObjective();
        }
    }
}
