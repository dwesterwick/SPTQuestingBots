using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class ToggleSwitchAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private EFT.Interactive.Switch switchObject = null;
        private Player player = null;

        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            Player player = BotOwner.GetPlayer;
            if (player == null)
            {
                throw new InvalidOperationException("Cannot get Player object from " + BotOwner.GetText());
            }

            switchObject = ObjectiveManager.CurrentQuestSwitch;
            if (switchObject == null)
            {
                throw new InvalidOperationException("Cannot toggle a null switch");
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

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                throw new InvalidOperationException("Cannot go to a null position");
            }

            ObjectiveManager.StartJobAssigment();

            if (switchObject.DoorState == EDoorState.Open)
            {
                LoggingController.LogWarning("Switch " + switchObject.Id + " is already open");

                ObjectiveManager.CompleteObjective();
                return;
            }

            Vector3 interactionPosition = switchObject.GetInteractionPosition(BotOwner.Position);
            /*Vector3? interactionNavMeshPosition = LocationController.FindNearestNavMeshPosition(interactionPosition, 1.5f);
            if (!interactionNavMeshPosition.HasValue)
            {
                LoggingController.LogError("Cannot find valid NavMesh position close to " + interactionPosition.ToString() + " for " + BotOwner.GetText() + " to interact with switch " + switchObject.Id);
                ObjectiveManager.FailObjective();
            }*/

            float distanceFromInteractionPosition = Vector3.Distance(BotOwner.Position, interactionPosition);
            if (distanceFromInteractionPosition > 0.75f)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " is " + Math.Round(distanceFromInteractionPosition, 2) + "m from interaction position for switch " + switchObject.Id);
                
                RecalculatePath(interactionPosition);
                return;
            }

            if ((switchObject.DoorState == EDoorState.Shut) && (switchObject.InteractingPlayer == null))
            {
                Action callback = () =>
                {
                    LoggingController.LogInfo(BotOwner.GetText() + " toggled switch " + switchObject.Id);
                };

                player.CurrentManagedState.ExecuteDoorInteraction(switchObject, new InteractionResult(EInteractionType.Open), callback, player);
                //player.CurrentManagedState.StartDoorInteraction(switchObject, new InteractionResult(EInteractionType.Open), callback);
                //player.MovementContext.ExecuteInteraction(switchObject, new InteractionResult(EInteractionType.Open));

                ObjectiveManager.CompleteObjective();
            }
            else
            {
                LoggingController.LogWarning("Somebody is already interacting with switch " + switchObject.Id);
            }
        }
    }
}
