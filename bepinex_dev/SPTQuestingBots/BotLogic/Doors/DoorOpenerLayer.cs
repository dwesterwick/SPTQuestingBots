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
    internal class DoorOpenerLayer : BehaviorExtensions.CustomLayerForQuesting
    {
        public DoorOpenerLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 100)
        {

        }

        public override string GetName()
        {
            return "DoorOpenerLayer";
        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        public override bool IsActive()
        {
            if (!canUpdate())
            {
                return previousState;
            }

            if ((BotOwner.BotState != EBotState.Active) || BotOwner.IsDead)
            {
                return updatePreviousState(false);
            }

            if (objectiveManager.DoorToOpen?.DoorState == EDoorState.Open)
            {
                LoggingController.LogInfo("Door " + objectiveManager.DoorToOpen.Id + " is now open");

                objectiveManager.DoorToOpen = null;
                return updatePreviousState(false);
            }
            
            if (objectiveManager.DoorToOpen == null)
            {
                objectiveManager.DoorToOpen = tryFindNearbyUnlockedDoor();
                return updatePreviousState(false);
            }

            setNextAction(BehaviorExtensions.BotActionType.OpenDoor, "OpenDoor");
            return updatePreviousState(true);
        }

        private Door tryFindNearbyUnlockedDoor()
        {
            IEnumerable<Door> lockedDoors = LocationController.FindLockedDoorsNearPosition(BotOwner.Position, 2f, false)
                .Where(d => d.DoorState == EDoorState.Shut);

            if (!lockedDoors.Any())
            {
                return null;
            }

            Door selectedDoor = lockedDoors.First();
            return selectedDoor;
        }
    }
}
