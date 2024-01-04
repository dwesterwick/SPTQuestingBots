using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BehaviorExtensions
{
    public abstract class CustomLogicDelayedUpdate : CustomLogic
    {
        protected BotLogic.Objective.BotObjectiveManager ObjectiveManager { get; private set; }
        protected GClass114 baseAction { get; private set; } = null;
        protected static int updateInterval { get; private set; } = 100;

        private Stopwatch updateTimer = Stopwatch.StartNew();
        private Stopwatch actionElapsedTime = new Stopwatch();

        // Find by CreateNode(BotLogicDecision type, BotOwner bot) -> case BotLogicDecision.simplePatrol -> private gclass object
        private GClass288 baseSteeringLogic = new GClass288();

        protected double ActionElpasedTime => actionElapsedTime.ElapsedMilliseconds / 1000.0;

        public CustomLogicDelayedUpdate(BotOwner botOwner) : base(botOwner)
        {
            ObjectiveManager = BotLogic.Objective.BotObjectiveManager.GetObjectiveManagerForBot(botOwner);
        }

        public CustomLogicDelayedUpdate(BotOwner botOwner, int delayInterval) : this(botOwner)
        {
            updateInterval = delayInterval;
        }

        public override void Start()
        {
            RestartActionElapsedTime();
        }

        public override void Stop()
        {
            actionElapsedTime.Stop();
        }

        public void RestartActionElapsedTime()
        {
            actionElapsedTime.Restart();
        }

        public void SetBaseAction(GClass114 _baseAction)
        {
            baseAction = _baseAction;
            baseAction.Awake();
        }

        public void UpdateBaseAction()
        {
            baseAction?.Update();
        }

        public void UpdateBotMovement(bool canSprint = true)
        {
            // Stand up
            BotOwner.SetPose(1f);

            // Move as fast as possible
            BotOwner.SetTargetMoveSpeed(1f);
            
            // Open doors blocking the bot's path
            BotOwner.DoorOpener.Update();

            // Disable sprinting if the bot is very close to its current destination point to prevent it from sliding into staircase corners, etc.
            if (Vector3.Distance(BotOwner.Position, BotOwner.Mover.RealDestPoint) < 1)
            {
                canSprint = false;
            }

            if (canSprint && BotOwner.GetPlayer.Physical.CanSprint && (BotOwner.GetPlayer.Physical.Stamina.NormalValue > 0.5f))
            {
                //Controllers.LoggingController.LogInfo(BotOwner.GetText() + " can sprint");
                BotOwner.GetPlayer.EnableSprint(true);
            }

            if (!canSprint || !BotOwner.GetPlayer.Physical.CanSprint || (BotOwner.GetPlayer.Physical.Stamina.NormalValue < 0.1f))
            {
                BotOwner.GetPlayer.EnableSprint(false);
            }
        }

        public void UpdateBotSteering()
        {
            BotOwner.Steering.LookToMovingDirection();
            baseSteeringLogic.Update(BotOwner);
        }

        public void UpdateBotSteering(Vector3 point)
        {
            BotOwner.Steering.LookToPoint(point);
            baseSteeringLogic.Update(BotOwner);
        }

        protected bool canUpdate()
        {
            if (updateTimer.ElapsedMilliseconds < updateInterval)
            {
                return false;
            }

            updateTimer.Restart();
            return true;
        }
    }
}
