using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
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

        public override void Update()
        {
            UpdateBotMovement(CanSprint);

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (ObjectiveManager.CurrentQuestSwitch == null)
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

            if (ObjectiveManager.CurrentQuestSwitch.DoorState == EDoorState.Open)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.CurrentQuestSwitch.Id + " is already open");

                ObjectiveManager.CompleteObjective();

                return;
            }

            if (ObjectiveManager.CurrentQuestSwitch.DoorState == EDoorState.Locked)
            {
                LoggingController.LogWarning("Switch " + ObjectiveManager.CurrentQuestSwitch.Id + " is unavailable");

                ObjectiveManager.TryChangeObjective();

                return;
            }

            if (checkIfBotIsStuck())
            {
                LoggingController.LogWarning(BotOwner.GetText() + " got stuck while trying to toggle switch " + ObjectiveManager.CurrentQuestSwitch.Id + ". Giving up.");

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }

                return;
            }

            // TO DO: Can this distance be reduced?
            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition > 0.75f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
                {
                    LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to switch " + ObjectiveManager.CurrentQuestSwitch.Id);

                    ObjectiveManager.FailObjective();

                    if (ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }

                return;
            }

            if (ObjectiveManager.CurrentQuestSwitch.DoorState == EDoorState.Shut)
            {
                toggleSwitch(ObjectiveManager.CurrentQuestSwitch, EInteractionType.Open);
            }
            else
            {
                LoggingController.LogWarning("Somebody is already interacting with switch " + ObjectiveManager.CurrentQuestSwitch.Id);
            }

            ObjectiveManager.CompleteObjective();
        }

        private void toggleSwitch(EFT.Interactive.Switch sw, EInteractionType interactionType)
        {
            try
            {
                if (sw == null)
                {
                    throw new ArgumentNullException(nameof(sw));
                }

                Player player = BotOwner.GetPlayer;
                if (player == null)
                {
                    throw new InvalidOperationException("Cannot get Player object from " + BotOwner.GetText());
                }

                player.MovementContext.ExecuteInteraction(sw, new InteractionResult(interactionType));
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
