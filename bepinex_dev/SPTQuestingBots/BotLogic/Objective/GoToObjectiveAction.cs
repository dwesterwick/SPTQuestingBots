using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic.Objective
{
    internal class GoToObjectiveAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public GoToObjectiveAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {

        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);

            // Don't allow expensive parts of this behavior (calculating a path to an objective) to run too often
            if (!canUpdate())
            {
                return;
            }

            // This doesn't really need to be updated every frame
            CanSprint = QuestingBotsPluginConfig.SprintingEnabled.Value && ObjectiveManager.CanSprintToObjective();

            if (!ObjectiveManager.IsQuestingAllowed || !ObjectiveManager.Position.HasValue)
            {
                return;
            }

            if (!ObjectiveManager.IsJobAssignmentActive)
            {
                return;
            }

            // Check if the bot just completed its objective
            if (ObjectiveManager.IsCloseToObjective())
            {
                if (ObjectiveManager.CurrentQuestAction == Models.QuestAction.MoveToPosition)
                {
                    ObjectiveManager.CompleteObjective();
                }

                //LoggingController.LogInfo(BotOwner.GetText() + " reached its objective (" + ObjectiveManager + ").");

                return;
            }

            ObjectiveManager.StartJobAssigment();

            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc. 
            tryMoveToObjective();

            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    ObjectiveManager.StuckCount++;
                    LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is stuck and will get a new objective.");
                }
                wasStuck = true;

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }
            }
            else
            {
                wasStuck = false;
            }
        }

        private bool tryMoveToObjective()
        {
            NavMeshPathStatus? pathStatus = RecalculatePath(ObjectiveManager.Position.Value);

            // Don't complete or fail the objective step except for the action type "MoveToPosition"
            if (ObjectiveManager.CurrentQuestAction != Models.QuestAction.MoveToPosition)
            {
                return true;
            }

            // If the path is invalid, there's nowhere for the bot to move
            if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
            {
                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " cannot find a path to " + ObjectiveManager);
                ObjectiveManager.FailObjective();
                return false;
            }

            if (pathStatus.HasValue && (pathStatus.Value == NavMeshPathStatus.PathPartial))
            {
                float distanceToEndOfPath = float.NaN;
                float distanceToObjective = float.NaN;
                float missingDistance = float.NaN;

                // Calculate distances between:
                //  - the bot and the end of the incomplete path to its objective
                //  - the bot and its objective step position
                //  - the end of its partial path and its objective step position
                Vector3? lastPathPoint = BotOwner.Mover?.CurPathLastPoint;
                if (lastPathPoint.HasValue)
                {
                    distanceToEndOfPath = Vector3.Distance(BotOwner.Position, lastPathPoint.Value);
                    distanceToObjective = Vector3.Distance(BotOwner.Position, ObjectiveManager.Position.Value);
                    missingDistance = Vector3.Distance(ObjectiveManager.Position.Value, lastPathPoint.Value);
                }

                // Check if the bot is nearly at the end of its (incomplete) path
                if (distanceToEndOfPath < ConfigController.Config.Questing.BotSearchDistances.MaxNavMeshPathError)
                {
                    if (distanceToObjective < ConfigController.Config.Questing.BotSearchDistances.ObjectiveReachedNavMeshPathError)
                    {
                        LoggingController.LogInfo("Bot " + BotOwner.GetText() + " cannot find a complete path to its objective (" + ObjectiveManager + "). Got close enough. Remaining distance to objective: " + distanceToObjective);
                        ObjectiveManager.CompleteObjective();
                        return true;
                    }

                    LoggingController.LogWarning("Bot " + BotOwner.GetText() + " cannot find a complete path to its objective (" + ObjectiveManager + "). Giving up. Remaining distance to objective: " + distanceToObjective);
                    ObjectiveManager.FailObjective();
                    return false;
                }

                // Check if this is the first time an incomplete path was generated. If so, write a warning message. 
                if (ObjectiveManager.HasCompletePath)
                {
                    LoggingController.LogWarning("Bot " + BotOwner.GetText() + " cannot find a complete path to its objective (" + ObjectiveManager + "). Trying anyway. Distance from end of path to objective: " + missingDistance);
                    ObjectiveManager.ReportIncompletePath();
                }
            }

            return true;
        }
    }
}
