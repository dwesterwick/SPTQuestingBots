using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Investigate
{
    internal class BotInvestigationLayer : CustomLayerForQuesting
    {
        private bool IsInCombat => ObjectiveManager.BotMonitor.GetMonitor<BotCombatMonitor>().IsInCombat;
        private bool IsHealing => BotOwner.Medecine.FirstAid.Using || BotOwner.Medecine.SurgicalKit.Using;
        private bool IsEatingOrDrinking => BotOwner.EatDrinkData.Using;

        private bool IsGesturing() => BotOwner.Gesture.CurRequestExecuting();

        public BotInvestigationLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 100)
        {
            
        }

        public override string GetName()
        {
            return "BotInvestigationLayer";
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
            if (!ObjectiveManager.IsQuestingAllowed)
            {
                return updatePreviousState(false);
            }

            if (!canUpdate())
            {
                return previousState;
            }

            BotHearingMonitor hearingMonitor = ObjectiveManager.BotMonitor.GetMonitor<BotHearingMonitor>();
            if (!hearingMonitor.IsSuspicious || (hearingMonitor.LastEstimatedSoundPosition == null))
            {
                return updatePreviousState(false);
            }

            if (!WantsToInvestigate())
            {
                return updatePreviousState(false);
            }

            float pauseRequestTime = getPauseRequestTime();
            if (pauseRequestTime > 0)
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Pausing layer for " + pauseRequestTime + "s...");
                return pauseLayer(pauseRequestTime);
            }

            if (hearingMonitor.WillInvestigateSounds && !hearingMonitor.IsAtLastEstimatedSoundPosition && hearingMonitor.CanGoToLastEstimatedSoundPosition())
            {
                setNextAction(BotActionType.InvestigateSound, "InvestigateSound");
                return updatePreviousState(true);
            }

            ObjectiveManager.CoverPointSelector.RefreshCoverPointIfStale();
            if (ObjectiveManager.CoverPointSelector.HasSelectedPoint)
            {
                if (!ObjectiveManager.CoverPointSelector.IsAtSelectedPoint)
                {
                    setNextAction(BotActionType.GetToCover, "GetToCover");
                    return updatePreviousState(true);
                }

                setNextAction(BotActionType.Recover, "ListenInCover");
                return updatePreviousState(true);
            }

            setNextAction(BotActionType.LookAtSound, "FreezeAndListen");
            return updatePreviousState(true);
        }

        private bool WantsToInvestigate()
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
    }
}
