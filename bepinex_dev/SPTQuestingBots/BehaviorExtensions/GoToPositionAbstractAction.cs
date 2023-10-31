using EFT;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal abstract class GoToPositionAbstractAction : CustomLogicDelayedUpdate
    {
        protected BotLogic.Objective.BotObjectiveManager ObjectiveManager;
        protected Stopwatch actionElapsedTime = new Stopwatch();
        protected bool CanSprint = true;

        public GoToPositionAbstractAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            getObjectiveManager();
        }

        public GoToPositionAbstractAction(BotOwner _BotOwner, int delayInterval) : base(_BotOwner, delayInterval)
        {
            getObjectiveManager();
        }

        public override void Start()
        {
            actionElapsedTime.Restart();
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            actionElapsedTime.Stop();
            BotOwner.PatrollingData.Unpause();
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position)
        {
            return RecalculatePath(position, 0.5f);
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position, float reachDist)
        {
            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc. 
            return BotOwner.Mover?.GoToPoint(position, true, reachDist, false, false);
        }

        private void getObjectiveManager()
        {
            ObjectiveManager = BotOwner.GetPlayer.gameObject.GetOrAddComponent<BotLogic.Objective.BotObjectiveManager>();
        }
    }
}
