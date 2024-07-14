using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SPTQuestingBots.BehaviorExtensions
{
    public enum BotActionType
    {
        Undefined,
        GoToObjective,
        FollowBoss,
        HoldPosition,
        Ambush,
        Snipe,
        PlantItem,
        BossRegroup,
        FollowerRegroup,
        Sleep,
        ToggleSwitch,
        UnlockDoor,
        CloseNearbyDoors
    }

    internal abstract class CustomLayerDelayedUpdate : CustomLayer
    {
        protected static int updateInterval { get; private set; } = 100;
        protected bool previousState { get; private set; } = false;
        
        private BotActionType nextAction = BotActionType.Undefined;
        private BotActionType previousAction = BotActionType.Undefined;
        private string actionReason = "???";
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private float pauseLayerTime = 0;
        
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
            //LoggingController.LogInfo(BotOwner.GetText() + " is swtiching from " + previousAction.ToString() + " to " + nextAction.ToString());

            previousAction = nextAction;

            switch (nextAction)
            {
                case BotActionType.GoToObjective: return new Action(typeof(BotLogic.Objective.GoToObjectiveAction), actionReason);
                case BotActionType.FollowBoss: return new Action(typeof(BotLogic.Follow.FollowBossAction), actionReason);
                case BotActionType.HoldPosition: return new Action(typeof(BotLogic.Objective.HoldAtObjectiveAction), actionReason);
                case BotActionType.Ambush: return new Action(typeof(BotLogic.Objective.AmbushAction), actionReason);
                case BotActionType.Snipe: return new Action(typeof(BotLogic.Objective.SnipeAction), actionReason);
                case BotActionType.PlantItem: return new Action(typeof(BotLogic.Objective.PlantItemAction), actionReason);
                case BotActionType.BossRegroup: return new Action(typeof(BotLogic.Follow.BossRegroupAction), actionReason);
                case BotActionType.FollowerRegroup: return new Action(typeof(BotLogic.Follow.FollowerRegroupAction), actionReason);
                case BotActionType.Sleep: return new Action(typeof(BotLogic.Sleep.SleepingAction), actionReason);
                case BotActionType.ToggleSwitch: return new Action(typeof(BotLogic.Objective.ToggleSwitchAction), actionReason);
                case BotActionType.UnlockDoor: return new Action(typeof(BotLogic.Objective.UnlockDoorAction), actionReason);
                case BotActionType.CloseNearbyDoors: return new Action(typeof(BotLogic.Objective.CloseNearbyDoorsAction), actionReason);
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
            previousState = false;
            pauseLayerTime = minTime;
            pauseLayerTimer.Restart();

            return false;
        }
    }
}
