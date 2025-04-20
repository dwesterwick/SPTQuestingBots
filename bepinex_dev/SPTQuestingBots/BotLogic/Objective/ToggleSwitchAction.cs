using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class ToggleSwitchAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, 100)
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

            if (ObjectiveManager.GetCurrentQuestInteractiveObject() == null)
            {
                LoggingController.LogError("Cannot toggle a null switch");

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

            // If the switch has already been toggled, there is nothing else for the bot to do
            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Open)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is already open");

                ObjectiveManager.CompleteObjective();

                return;
            }

            // If players are unable to toggle the switch, the bot shouldn't be allowed either
            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Locked)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id + " is unavailable");

                ObjectiveManager.TryChangeObjective();

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

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition > 0.75f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                //if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
                if (!pathStatus.HasValue || (BotOwner.Mover?.IsPathComplete(ObjectiveManager.Position.Value, 0.5f) != true))
                {
                    LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);

                    ObjectiveManager.FailObjective();

                    if (ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }

                return;
            }

            if (ObjectiveManager.GetCurrentQuestInteractiveObject().DoorState == EDoorState.Shut)
            {
                BotOwner.ToggleSwitch(ObjectiveManager.GetCurrentQuestInteractiveObject(), EInteractionType.Open);
            }
            else
            {
                // Presumably, if somebody is interacting with the switch, there is nothing else the bot needs to do for this objective
                LoggingController.LogWarning("Somebody is already interacting with switch " + ObjectiveManager.GetCurrentQuestInteractiveObject().Id);
            }

            ObjectiveManager.CompleteObjective();
        }
    }
}
