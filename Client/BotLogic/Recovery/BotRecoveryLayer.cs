using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Recovery
{
    internal class BotRecoveryLayer : CustomLayerForQuesting
    {
        private bool IsInCombat => ObjectiveManager.BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat;
        private bool IsHealing => BotOwner.Medecine.FirstAid.Using || BotOwner.Medecine.SurgicalKit.Using;
        private bool IsEatingOrDrinking => BotOwner.EatDrinkData.Using;

        private bool IsGesturing() => BotOwner.Gesture.CurRequestExecuting();

        public BotRecoveryLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 25)
        {

        }

        public override string GetName()
        {
            return "BotRecoveryLayer";
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

            if (ObjectiveManager.PatrolPointSelector.HasPatrolPoint && !ObjectiveManager.PatrolPointSelector.IsWayReserved)
            {
                //return updatePreviousState(false);
            }

            if (!ObjectiveManager.CoverPointSelector.HasCoverPoint)
            {
                if (previousState)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " no longer has a nearby cover point");
                }

                return updatePreviousState(false);
            }

            if (!ObjectiveManager.CoverPointSelector.IsAtCoverPoint && CanMoveToCoverPoint())
            {
                setNextAction(BotActionType.GetToCover, "GetToCover");
                return updatePreviousState(true);
            }

            if (ShouldHeal())
            {
                setNextAction(BotActionType.Heal, "Heal");
                return updatePreviousState(true);
            }

            if (ShouldEatOrDrink())
            {
                setNextAction(BotActionType.EatDrink, "EatDrink");
                return updatePreviousState(true);
            }

            if (ShouldGesture())
            {
                setNextAction(BotActionType.Gesture, "Gesture");
                return updatePreviousState(true);
            }

            setNextAction(BotActionType.Recover, "Recover");
            return updatePreviousState(true);
        }

        private bool CanMoveToCoverPoint()
        {
            if (IsHealing)
            {
                return false;
            }

            if (IsEatingOrDrinking)
            {
                return false;
            }

            if (IsGesturing())
            {
                return false;
            }

            return true;
        }

        private bool ShouldHeal()
        {
            if (IsInCombat)
            {
                return false;
            }

            if (IsHealing)
            {
                return true;
            }

            if (BotOwner.Medecine.FirstAid.Have2Do && BotOwner.Medecine.FirstAid.HaveSmth2Use)
            {
                return true;
            }

            if (BotOwner.Medecine.SurgicalKit.HaveWork && BotOwner.Medecine.SurgicalKit.HaveSmth2Use)
            {
                return true;
            }

            return false;
        }

        private bool ShouldEatOrDrink()
        {
            if (IsInCombat)
            {
                return false;
            }

            if (IsEatingOrDrinking || BotOwner.EatDrinkData.HaveActions())
            {
                return true;
            }

            return false;
        }

        private bool ShouldGesture()
        {
            if (IsInCombat)
            {
                return false;
            }

            if (IsGesturing() || BotOwner.Gesture.HaveRequest())
            {
                return true;
            }

            return false;
        }
    }
}
