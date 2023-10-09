using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SPTQuestingBots.BotLogic
{
    public abstract class CustomLogicDelayedUpdate : CustomLogic
    {
        // Find by CreateNode(BotLogicDecision type, BotOwner bot) -> case BotLogicDecision.simplePatrol -> private gclass object
        private GClass288 baseSteeringLogic = new GClass288();

        private Stopwatch updateTimer = Stopwatch.StartNew();
        private int updateInterval = 100;

        public CustomLogicDelayedUpdate(BotOwner botOwner) : base(botOwner)
        {

        }

        public CustomLogicDelayedUpdate(BotOwner botOwner, int delayInterval) : this(botOwner)
        {
            updateInterval = delayInterval;
        }

        public void UpdateBotMovement(bool canSprint = true)
        {
            // Look where you're going
            BotOwner.SetPose(1f);
            BotOwner.Steering.LookToMovingDirection();
            BotOwner.SetTargetMoveSpeed(1f);
            baseSteeringLogic.Update(BotOwner);

            BotOwner.DoorOpener.Update();

            if (canSprint && BotOwner.GetPlayer.Physical.CanSprint && (BotOwner.GetPlayer.Physical.Stamina.NormalValue > 0.5f))
            {
                BotOwner.GetPlayer.EnableSprint(true);
            }

            if (!canSprint || !BotOwner.GetPlayer.Physical.CanSprint || (BotOwner.GetPlayer.Physical.Stamina.NormalValue < 0.1f))
            {
                BotOwner.GetPlayer.EnableSprint(false);
            }
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
