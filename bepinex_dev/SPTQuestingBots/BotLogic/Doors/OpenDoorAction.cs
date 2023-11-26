using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Interactive;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.Doors
{
    internal class OpenDoorAction : BehaviorExtensions.CustomLogicDelayedUpdate
    {
        public OpenDoorAction(BotOwner _BotOwner) : base(_BotOwner, 20)
        {
            SetBaseAction(GClass394.CreateNode(BotLogicDecision.holdPosition, BotOwner));
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
            BotOwner.Mover.Stop();
            UpdateBaseAction();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (ObjectiveManager.DoorToOpen == null)
            {
                LoggingController.LogError("Door assigned for " + BotOwner.GetText() + " to open is null");
                return;
            }

            if (ObjectiveManager.DoorToOpen.DoorState == EDoorState.Open)
            {
                LoggingController.LogInfo("Door " + ObjectiveManager.DoorToOpen.Id + " is open");
                return;
            }

            if (ObjectiveManager.DoorToOpen.DoorState == EDoorState.Shut)
            {
                LoggingController.LogInfo(BotOwner.GetText() + " will open door " + ObjectiveManager.DoorToOpen.Id + "...");
                BotOwner.DoorOpener.Interact(ObjectiveManager.DoorToOpen, EInteractionType.Open);
            }
            else
            {
                //LoggingController.LogInfo("Forcing door " + ObjectiveManager.DoorToOpen.Id + " to open...");
                ObjectiveManager.DoorToOpen.Interact(new InteractionResult(EInteractionType.Open));
            }
        }
    }
}
