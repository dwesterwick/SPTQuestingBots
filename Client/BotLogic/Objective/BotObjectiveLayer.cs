using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Configuration;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.Objective
{
    internal class BotObjectiveLayer : CustomLayerForQuesting
    {
        private bool _isWaitingInCover = false;

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
                return PreviousState;
            }

            BotQuestingDecisionMonitor? decisionMonitor = ObjectiveManager.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
            if (decisionMonitor == null)
            {
                return updatePreviousState(false);
            }

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
            if (shouldRegroup())
            {
                _isWaitingInCover = shouldWaitForGroupInCover();
                if (_isWaitingInCover)
                {
                    ObjectiveManager.CoverPointSelector.RefreshCoverPointIfStale();
                    if (ObjectiveManager.CoverPointSelector.HasSelectedPoint)
                    {
                        setNextAction(BotActionType.GetToCover, "BossWaitForGroup");
                        return updatePreviousState(true);
                    }
                }

                setNextAction(BotActionType.BossRegroup, "BossRegroup");
                return updatePreviousState(true);
            }

            if (decisionMonitor.CurrentDecision != EBotQuestingDecision.Quest)
            {
                return updatePreviousState(false);
            }

            // Determine what type of action is needed for the bot to complete its assignment
            bool willQuest = trySetNextAction();
            if (willQuest)
            {
                informFollowersIfRestartingQuesting();
            }

            return willQuest;
        }

        private void informFollowersIfRestartingQuesting()
        {
            if (PreviousState)
            {
                return;
            }

            foreach (BotOwner follower in BotHiveMindMonitor.GetGroupFollowers(BotOwner))
            {
                BotQuestingDecisionMonitor? questingDecisionMonitor = follower.GetObjectiveManager()?.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
                if (questingDecisionMonitor == null)
                {
                    continue;
                }

                questingDecisionMonitor.BossHasRestartedQuesting = true;
            }
        }

        private bool shouldRegroup()
        {
            BotQuestingDecisionMonitor? decisionMonitor = ObjectiveManager.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
            if (decisionMonitor == null)
            {
                return false;
            }

            if (decisionMonitor.CurrentDecision == EBotQuestingDecision.Regroup)
            {
                writeRegroupDebugMessage();
                return true;
            }

            float minRegroupTime = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.MinRegroupTime;
            if (PreviousState && (PreviousAction == BotActionType.BossRegroup) && (LogicActiveTime < minRegroupTime))
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Keeping " + BotOwner.GetText() + " in BossRegroup because the layer has only been active for " + LogicActiveTime + "s");
                return true;
            }

            return false;
        }

        private void writeRegroupDebugMessage()
        {
            if (LogicActiveTime < 10)
            {
                return;
            }

            BotQuestingMonitor questingMonitor = ObjectiveManager.BotMonitor!.GetMonitor<BotQuestingMonitor>();
            string message = BotOwner.GetText() + " has been regrouping for " + LogicActiveTime + "s.";
            if (questingMonitor.FollowerDistanceRangeOverall != null)
            {
                message += " Overall: " + Math.Round(questingMonitor.FollowerDistanceRangeOverall.Min, 2) + "-" + Math.Round(questingMonitor.FollowerDistanceRangeOverall.Max, 2);
            }
            if (questingMonitor.FollowerDistanceRangeFollowing != null)
            {
                message += " Following: " + Math.Round(questingMonitor.FollowerDistanceRangeFollowing.Min, 2) + "-" + Math.Round(questingMonitor.FollowerDistanceRangeFollowing.Max, 2);
            }
            Singleton<LoggingUtil>.Instance.LogDebug(message);
        }

        private bool shouldWaitForGroupInCover()
        {
            MinMaxConfig? followerDistanceRange = ObjectiveManager?.BotMonitor?.GetMonitor<BotQuestingMonitor>()?.FollowerDistanceRangeOverall;
            if (followerDistanceRange == null)
            {
                return false;
            }

            if (followerDistanceRange.Min < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Nearest)
            {
                return true;
            }

            if (_isWaitingInCover && (followerDistanceRange.Min < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxFollowerDistance.Furthest))
            {
                return true;
            }

            return false;
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
