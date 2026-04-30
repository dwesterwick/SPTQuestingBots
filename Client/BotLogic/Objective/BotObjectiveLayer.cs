using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            BotQuestingDecisionMonitor decisionMonitor = objectiveManager.BotMonitor.GetMonitor<BotQuestingDecisionMonitor>();

            if (!decisionMonitor.IsAllowedToQuest())
            {
                return updatePreviousState(false);
            }

            if (decisionMonitor.HasAQuestingBoss && !objectiveManager.PrioritizeQuestingOverFollowing)
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
            switch (objectiveManager.CurrentQuestAction)
            {
                case QuestAction.MoveToPosition:
                    if (objectiveManager.MustUnlockDoor)
                    {
                        string interactiveObjectShortID = objectiveManager.GetCurrentQuestInteractiveObject().Id.Abbreviate();
                        setNextAction(BotActionType.UnlockDoor, "UnlockDoor (" + interactiveObjectShortID + ")");
                    }
                    else
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToObjective");
                    }
                    return updatePreviousState(true);

                case QuestAction.Teleport:
                    if (!objectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToTeleportPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Teleport, "Teleport");
                    }
                    return updatePreviousState(true);

                case QuestAction.HoldAtPosition:
                    setNextAction(BotActionType.HoldPosition, "HoldPosition (" + objectiveManager.MinElapsedActionTime + "s)");
                    return updatePreviousState(true);

                case QuestAction.Ambush:
                    if (!objectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToAmbushPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Ambush, "Ambush (" + objectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.Snipe:
                    if (!objectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToSnipePosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.Snipe, "Snipe (" + objectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.PlantItem:
                    if (!objectiveManager.IsCloseToObjective())
                    {
                        setNextAction(BotActionType.GoToObjective, "GoToPlantPosition");
                    }
                    else
                    {
                        setNextAction(BotActionType.PlantItem, "PlantItem (" + objectiveManager.MinElapsedActionTime + "s)");
                    }
                    return updatePreviousState(true);

                case QuestAction.ToggleSwitch:
                    if (!objectiveManager.IsCloseToObjective())
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
                    if (objectiveManager.BotMonitor.GetMonitor<BotExtractMonitor>().TryInstructBotToExtract())
                    {
                        objectiveManager.StopQuesting();
                    }
                    objectiveManager.CompleteObjective();
                    return updatePreviousState(true);
            }

            // Failsafe
            return updatePreviousState(false);
        }
    }
}
