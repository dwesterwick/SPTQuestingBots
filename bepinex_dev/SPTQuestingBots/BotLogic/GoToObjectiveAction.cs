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
using UnityEngine.AI;

namespace SPTQuestingBots.BotLogic
{
    internal class GoToObjectiveAction : CustomLogic
    {
        private BotObjective objective;
        private BotOwner botOwner;
        private GClass274 baseSteeringLogic = new GClass274();
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private int updateInterval = 100;
        
        public GoToObjectiveAction(BotOwner _botOwner) : base(_botOwner)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.GetComponent<BotObjective>();
        }

        public override void Start()
        {
            botOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            botOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            // Look where you're going
            botOwner.SetPose(1f);
            botOwner.Steering.LookToMovingDirection();
            botOwner.SetTargetMoveSpeed(1f);
            baseSteeringLogic.Update(botOwner);

            botOwner.DoorOpener.Update();

            if (botOwner.GetPlayer.Physical.CanSprint && (botOwner.GetPlayer.Physical.Stamina.NormalValue > 0.5f))
            {
                botOwner.GetPlayer.EnableSprint(true);
            }
            if (!botOwner.GetPlayer.Physical.CanSprint ||(botOwner.GetPlayer.Physical.Stamina.NormalValue < 0.1f))
            {
                botOwner.GetPlayer.EnableSprint(false);
            }

            if (!objective.IsObjectiveActive || !objective.Position.HasValue)
            {
                return;
            }

            if (!objective.CanReachObjective)
            {
                return;
            }

            if (updateTimer.ElapsedMilliseconds < updateInterval)
            {
                return;
            }
            updateTimer.Restart();

            if (!objective.IsObjectiveReached && Vector3.Distance(objective.Position.Value, botOwner.Position) < ConfigController.Config.BotSearchDistances.OjectiveReachedIdeal)
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " reached its objective (" + objective + ").");
                objective.CompleteObjective();
            }
            else
            {
                NavMeshPathStatus? pathStatus = botOwner.Mover?.GoToPoint(objective.Position.Value, true, 0.5f, false, false);
                if (!pathStatus.HasValue || (pathStatus.Value == NavMeshPathStatus.PathInvalid))
                {
                    LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot find a path to " + objective);
                    objective.RejectObjective();
                }
                if (pathStatus.HasValue && (pathStatus.Value == NavMeshPathStatus.PathPartial))
                {
                    Vector3? lastPathPoint = botOwner.Mover?.CurPathLastPoint;

                    float missingDistance = float.NaN;
                    float distanceToEndOfPath = float.NaN;
                    float distanceToObjective = float.NaN;
                    if (lastPathPoint.HasValue)
                    {
                        missingDistance = Vector3.Distance(objective.Position.Value, lastPathPoint.Value);
                        distanceToEndOfPath = Vector3.Distance(botOwner.Position, lastPathPoint.Value);
                        distanceToObjective = Vector3.Distance(botOwner.Position, objective.Position.Value);
                    }

                    if (distanceToEndOfPath < ConfigController.Config.BotSearchDistances.MaxNavMeshPathError)
                    {
                        if (distanceToObjective < ConfigController.Config.BotSearchDistances.ObjectiveReachedNavMeshPathError)
                        {
                            LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Got close enough. Remaining distance to objective: " + distanceToObjective);
                            objective.CompleteObjective();
                        }
                        else
                        {
                            LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Giving up. Remaining distance to objective: " + distanceToObjective);
                            objective.RejectObjective();
                        }

                        return;
                    }

                    if (objective.IsObjectivePathComplete)
                    {
                        LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " cannot find a complete path to its objective (" + objective + "). Trying anyway. Distance from end of path to objective: " + missingDistance);
                    }

                    objective.IsObjectivePathComplete = false;
                }

                if (!objective.CanReachObjective && objective.CanChangeObjective)
                {
                    objective.TryChangeObjective();
                }
            }
        }
    }
}
