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
        private EFT.Interactive.Switch switchObject = null;
        private Player player = null;

        public ToggleSwitchAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            player = BotOwner.GetPlayer;
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

            if (checkIfBotIsStuck())
            {
                LoggingController.LogWarning(BotOwner.GetText() + " got stuck while trying to toggle switch " + switchObject.Id + ". Giving up.");
                ObjectiveManager.FailObjective();
                return;
            }

            float distanceToTargetPosition = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
            if (distanceToTargetPosition > 0.75f)
            {
                NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

                if (!pathStatus.HasValue || (pathStatus.Value != NavMeshPathStatus.PathComplete))
                {
                    LoggingController.LogWarning(BotOwner.GetText() + " cannot find a complete path to switch " + switchObject.Id);

                    ObjectiveManager.FailObjective();

                    if (ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        drawBotPath(Color.yellow);
                    }
                }
                else
                {
                    //LoggingController.LogInfo(BotOwner.GetText() + " is " + Math.Round(distanceToTargetPosition, 2) + "m from interaction position for switch " + switchObject.Id);
                }

                return;
            }

            if ((switchObject.DoorState == EDoorState.Shut) && (switchObject.InteractingPlayer == null))
            {
                try
                {
                    Action callback = switchToggledAction(BotOwner, switchObject);
                    player.CurrentManagedState.ExecuteDoorInteraction(switchObject, new InteractionResult(EInteractionType.Open), callback, player);

                    ObjectiveManager.CompleteObjective();
                }
                catch (Exception e)
                {
                    LoggingController.LogError(BotOwner.GetText() + " cannot toggle switch " + switchObject.Id + ": " + e.Message);
                    LoggingController.LogError(e.StackTrace);
                    throw;
                }
            }
            else
            {
                LoggingController.LogWarning("Somebody is already interacting with switch " + switchObject.Id);
            }
        }

        public static Action switchToggledAction(BotOwner bot, Switch sw)
        {
            return () => { LoggingController.LogInfo(bot.GetText() + " toggled switch " + sw.Id); };
        }
    }
}
