using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic
{
    internal abstract class GoToPositionAbstractAction : BehaviorExtensions.CustomLogicDelayedUpdate
    {
        protected Objective.BotObjectiveManager ObjectiveManager;
        protected bool CanSprint = true;

        public GoToPositionAbstractAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            ObjectiveManager = BotOwner.GetPlayer.gameObject.GetOrAddComponent<Objective.BotObjectiveManager>();
        }

        public override void Start()
        {
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            BotOwner.PatrollingData.Unpause();
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position)
        {
            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc. 
            return BotOwner.Mover?.GoToPoint(position, true, 0.5f, false, false);
        }
    }
}
