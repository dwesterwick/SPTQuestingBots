using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;

namespace QuestingBots.BotLogic.Objective
{
    internal class BotObjectiveLayer : CustomLayerForQuesting
    {
        public BotObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 25)
        {
            
        }

        public override string GetName()
        {
            return "BotObjectiveLayer";
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

            BotQuestingDecisionMonitor decisionMonitor = ObjectiveManager.BotMonitor.GetMonitor<BotQuestingDecisionMonitor>();

            if (!decisionMonitor.IsAllowedToQuest())
            {
                return updatePreviousState(false);
            }

            if (decisionMonitor.ShouldFollowBoss())
            {
                return updatePreviousState(false);
            }

            float pauseRequestTime = getPauseRequestTime();
            if (pauseRequestTime > 0)
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Pausing layer for " + pauseRequestTime + "s...");
                return pauseLayer(pauseRequestTime);
            }

            // Check if the bot has wandered too far from its followers
            if (decisionMonitor.CurrentDecision == BotQuestingDecision.Regroup)
            {
                setNextAction(BotActionType.BossRegroup, "BossRegroup");
                return updatePreviousState(true);
            }

            if (decisionMonitor.CurrentDecision != BotQuestingDecision.Quest)
            {
                return updatePreviousState(false);
            }

            // Determine what type of action is needed for the bot to complete its assignment
            return updatePreviousState(trySetNextAction());
        }

        private bool trySetNextAction()
        {
            switch (ObjectiveManager.CurrentQuestAction)
            {
                case QuestAction.MoveToPosition:
                    if (ObjectiveManager.MustUnlockDoor)
                    {
                        string interactiveObjectShortID = ObjectiveManager.GetCurrentQuestInteractiveObject().Id.Abbreviate();
                        setNextAction(BotActionType.UnlockDoor, "UnlockDoor (" + interactiveObjectShortID + ")");
                    }
                    else
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToObjective");
                    }
                    return updatePreviousState(true);

                case QuestAction.Teleport:
                    if (!ObjectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToTeleportPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Teleport, "Teleport");
                    }
                    return updatePreviousState(true);

                case QuestAction.HoldAtPosition:
                    setNextAction(BotActionType.HoldPosition, "HoldPosition (" + ObjectiveManager.MinElapsedActionTime + "s)");
                    return updatePreviousState(true);

                case QuestAction.Ambush:
                    if (!ObjectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToAmbushPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Ambush, "Ambush (" + ObjectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.Snipe:
                    if (!ObjectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToSnipePosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Snipe, "Snipe (" + ObjectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.PlantItem:
                    if (!ObjectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToPlantPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.PlantItem, "PlantItem (" + ObjectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.ToggleSwitch:
                    if (!ObjectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToSwitchPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.ToggleSwitch, "ToggleSwitch");
                    }
                    return updatePreviousState(true);

                case QuestAction.CloseNearbyDoors:
                    setNextAction(BotActionType.CloseNearbyDoors, "CloseNearbyDoors");
                    return updatePreviousState(true);

                case QuestAction.OpenNearbyDoors:
                    setNextAction(BotActionType.CloseNearbyDoors, "OpenNearbyDoors");
                    return updatePreviousState(true);

                case QuestAction.RequestExtract:
                    if (ObjectiveManager.BotMonitor.GetMonitor<BotExtractMonitor>().TryInstructBotToExtract())
                    {
                        ObjectiveManager.StopQuesting();
                    }
                    ObjectiveManager.CompleteObjective();
                    return updatePreviousState(true);
            }

            // Failsafe
            return updatePreviousState(false);
        }
    }
}
