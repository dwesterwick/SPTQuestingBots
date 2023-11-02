using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.BotLogic.Follow;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.BotLogic.Sleep;

namespace SPTQuestingBots.BehaviorExtensions
{
    public enum BotActionType
    {
        Undefined,
        GoToObjective,
        FollowBoss,
        HoldPosition,
        Regroup,
        Sleep
    }

    internal abstract class CustomLayerDelayedUpdate : CustomLayer
    {
        protected bool previousState { get; private set; } = false;
        protected bool wasStuck { get; set; } = false;

        private BotActionType nextAction = BotActionType.Undefined;
        private BotActionType previousAction = BotActionType.Undefined;
        private string actionReason = "???";
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private Stopwatch botIsStuckTimer = Stopwatch.StartNew();
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private float pauseLayerTime = 0;
        private int updateInterval = 100;

        protected double StuckTime => botIsStuckTimer.ElapsedMilliseconds / 1000.0;

        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {

        }

        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority, int delayInterval) : this(_botOwner, _priority)
        {
            updateInterval = delayInterval;
        }

        public override bool IsCurrentActionEnding()
        {
            return nextAction != previousAction;
        }

        public override Action GetNextAction()
        {
            previousAction = nextAction;

            switch (nextAction)
            {
                case BotActionType.GoToObjective: return new Action(typeof(GoToObjectiveAction), actionReason);
                case BotActionType.FollowBoss: return new Action(typeof(FollowBossAction), actionReason);
                case BotActionType.HoldPosition: return new Action(typeof(HoldAtObjectiveAction), actionReason);
                case BotActionType.Regroup: return new Action(typeof(RegroupAction), actionReason);
                case BotActionType.Sleep: return new Action(typeof(SleepingAction), actionReason);
            }

            throw new InvalidOperationException("Invalid action selected for layer");
        }

        protected void setNextAction(BotActionType actionType, string reason)
        {
            nextAction = actionType;
            actionReason = reason;
        }

        protected bool canUpdate()
        {
            if (updateTimer.ElapsedMilliseconds < updateInterval)
            {
                return false;
            }

            if (pauseLayerTimer.ElapsedMilliseconds < 1000 * pauseLayerTime)
            {
                return false;
            }

            updateTimer.Restart();
            return true;
        }

        protected bool updatePreviousState(bool newState)
        {
            previousState = newState;
            return previousState;
        }

        protected bool pauseLayer()
        {
            return pauseLayer(0);
        }

        protected bool pauseLayer(float minTime)
        {
            pauseLayerTime = minTime;
            pauseLayerTimer.Restart();

            restartStuckTimer();

            return false;
        }

        protected void restartStuckTimer()
        {
            botIsStuckTimer.Restart();
        }
    }
}
