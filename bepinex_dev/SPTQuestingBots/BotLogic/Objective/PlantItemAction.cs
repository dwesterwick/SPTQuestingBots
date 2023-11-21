using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class PlantItemAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private Vector3? dangerPoint;

        public PlantItemAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass394.CreateNode(BotLogicDecision.lay, BotOwner));
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
            UpdateBaseAction();
            
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

            if (!ObjectiveManager.IsCloseToObjective())
            {
                dangerPoint = null;

                RecalculatePath(ObjectiveManager.Position.Value);
                UpdateBotSteering();
                RestartActionElapsedTime();

                return;
            }

            if (!dangerPoint.HasValue)
            {
                dangerPoint = findDangerPoint();
            }
            if (!dangerPoint.HasValue)
            {
                LoggingController.LogError("Cannot instruct bot to look at a null point");
                return;
            }

            UpdateBotSteering(dangerPoint.Value);

            if (ActionElpasedTime >= ObjectiveManager.MinElapsedActionTime)
            {
                ObjectiveManager.CompleteObjective();
            }
        }

        private Vector3? findDangerPoint()
        {
            IEnumerable<BotOwner> aliveBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead);

            int botCount = aliveBots.Count();

            if (botCount == 0)
            {
                return null;
            }

            Vector3 dangerPoint = Vector3.zero;
            foreach (BotOwner bot in aliveBots)
            {
                dangerPoint += bot.Position;
            }
            dangerPoint /= botCount;

            return dangerPoint;
        }
    }
}
