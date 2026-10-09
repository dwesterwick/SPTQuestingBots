using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace QuestingBots.BehaviorExtensions
{
    public enum BotActionType
    {
        Undefined,
        GoToObjective,
        Teleport,
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
        CloseNearbyDoors,
        OpenNearbyDoors,
        GetToCover,
        InvestigateSound,
        LookAtSound,
        Recover,
        Heal,
        EatDrink,
        Gesture
    }

    internal abstract class CustomLayerDelayedUpdate : CustomLayer
    {
        protected int UpdateInterval { get; private set; } = 100;
        protected bool PreviousState { get; private set; } = false;
        protected BotActionType NextAction { get; private set; } = BotActionType.Undefined;
        protected BotActionType PreviousAction { get; private set; } = BotActionType.Undefined;

        private string actionReason = "???";
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private Stopwatch layerActiveTimer = new Stopwatch();
        private float pauseLayerTime = 0;

        protected double LayerActiveTime => layerActiveTimer.ElapsedMilliseconds / 1000.0;
        
        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            
        }

        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority, int delayInterval) : this(_botOwner, _priority)
        {
            UpdateInterval = delayInterval;
        }

        public override bool IsCurrentActionEnding()
        {
            return NextAction != PreviousAction;
        }

        public override Action GetNextAction()
        {
            PreviousAction = NextAction;

            switch (NextAction)
            {
                case BotActionType.GoToObjective: return new Action(typeof(BotLogic.Objective.GoToObjectiveAction), actionReason);
                case BotActionType.Teleport: return new Action(typeof(BotLogic.Objective.TeleportAction), actionReason);
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
                case BotActionType.OpenNearbyDoors: return new Action(typeof(BotLogic.Objective.OpenNearbyDoorsAction), actionReason);
                case BotActionType.GetToCover: return new Action(typeof(BotLogic.Recovery.GetToCoverAction), actionReason);
                case BotActionType.InvestigateSound: return new Action(typeof(BotLogic.Investigate.BotInvestigateSoundAction), actionReason);
                case BotActionType.LookAtSound: return new Action(typeof(BotLogic.Investigate.LookAtSoundAction), actionReason);
                case BotActionType.Recover: return new Action(typeof(BotLogic.Recovery.BotRecoveryAction), actionReason);
                case BotActionType.Heal: return new Action(typeof(BotLogic.EftActions.EftHealAction), actionReason);
                case BotActionType.EatDrink: return new Action(typeof(BotLogic.EftActions.EftEatDrinkAction), actionReason);
                case BotActionType.Gesture: return new Action(typeof(BotLogic.EftActions.EftGestureAction), actionReason);
            }

            throw new InvalidOperationException("Invalid action selected for layer");
        }

        protected void setNextAction(BotActionType actionType, string reason)
        {
            NextAction = actionType;
            actionReason = reason;
        }

        protected bool canUpdate()
        {
            if (updateTimer.ElapsedMilliseconds < UpdateInterval)
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
            if (newState)
            {
                layerActiveTimer.Start();
            }
            else
            {
                layerActiveTimer.Reset();
            }

            PreviousState = newState;
            return PreviousState;
        }

        protected bool pauseLayer()
        {
            return pauseLayer(0);
        }

        protected bool pauseLayer(float minTime)
        {
            PreviousState = false;
            pauseLayerTime = minTime;
            pauseLayerTimer.Restart();
            layerActiveTimer.Reset();

            return false;
        }
    }
}
