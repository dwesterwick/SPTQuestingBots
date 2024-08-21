using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BehaviorExtensions
{
    public abstract class GoToPositionAbstractAction : CustomLogicDelayedUpdate
    {
        protected bool CanSprint { get; set; } = true;

        private Stopwatch botIsStuckTimer = new Stopwatch();
        private Stopwatch timeSinceLastJumpTimer = Stopwatch.StartNew();
        private Stopwatch timeSinceLastVaultTimer = Stopwatch.StartNew();
        private Vector3? lastBotPosition = null;

        protected double StuckTime => botIsStuckTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSinceLastJump => timeSinceLastJumpTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSinceLastVault => timeSinceLastVaultTimer.ElapsedMilliseconds / 1000.0;

        public GoToPositionAbstractAction(BotOwner _BotOwner, int delayInterval) : base(_BotOwner, delayInterval)
        {
            
        }

        public GoToPositionAbstractAction(BotOwner _BotOwner) : this(_BotOwner, updateInterval)
        {

        }

        public override void Start()
        {
            base.Start();

            timeSinceLastJumpTimer.Restart();
            botIsStuckTimer.Start();
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            base.Stop();

            botIsStuckTimer.Stop();
            BotOwner.PatrollingData.Unpause();
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position)
        {
            return RecalculatePath(position, 0.5f);
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position, float reachDist, bool force = false)
        {
            // If a bot is jumping or vaulting, recalculate its path after it finishes
            if (BotOwner.GetPlayer.MovementContext.PlayerAnimatorIsJumpSetted() || BotOwner.GetPlayer.MovementContext.PlayerAnimatorGetIsVaulting())
            {
                ObjectiveManager.BotPath.ForcePathRecalculation();
                return ObjectiveManager.BotPath.Status;
            }

            BotPathUpdateNeededReason updateReason = ObjectiveManager.BotPath.CheckIfUpdateIsNeeded(position, reachDist, force);

            if (ObjectiveManager.BotPath.Status != NavMeshPathStatus.PathInvalid)
            {
                if (updateReason != BotPathUpdateNeededReason.None)
                {
                    /*if (!ObjectiveManager.BotMonitor.IsFollowing() && !ObjectiveManager.BotMonitor.IsRegrouping())
                    {
                        LoggingController.LogInfo("Set " + ObjectiveManager.BotPath.Status.ToString() + " path to " + ObjectiveManager.BotPath.TargetPosition + " for " + BotOwner.GetText() + " due to " + updateReason.ToString());
                    }*/

                    BotOwner.FollowPath(ObjectiveManager.BotPath, true, false);
                }
            }
            else
            {
                BotOwner.Mover?.Stop();
            }

            return ObjectiveManager.BotPath.Status;
        }

        public NavMeshPathStatus? RecalculatePath_OLD(Vector3 position, float reachDist)
        {
            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc.
            NavMeshPathStatus? pathStatus = BotOwner.Mover?.GoToPoint(position, true, reachDist, false, false);

            return pathStatus;
        }

        protected void tryJump(bool useEFTMethod = true, bool force = false)
        {
            MovementContext movementContext = BotOwner.GetPlayer.MovementContext;

            if (useEFTMethod)
            {
                movementContext.TryJump();
                return;
            }

            if (movementContext.CanJump || force)
            {
                movementContext.method_2(1f);
                movementContext.PlayerAnimatorEnableJump(true);
            }
        }

        protected void restartStuckTimer()
        {
            botIsStuckTimer.Restart();
        }

        protected bool checkIfBotIsStuck()
        {
            return checkIfBotIsStuck(ConfigController.Config.Questing.StuckBotDetection.Time, true);
        }

        protected bool checkIfBotIsStuck(float stuckTime, bool drawPath)
        {
            updateBotStuckDetection();

            // If the bot hasn't moved enough within a certain time while this layer is active, assume the bot is stuck
            if (StuckTime > stuckTime)
            {
                if (drawPath && ConfigController.Config.Debug.ShowFailedPaths)
                {
                    drawBotPath(Color.red);
                }

                return true;
            }

            // If the bot might be stuck but stuckTime hasn't been reached, see if the we can stop the bot from being stuck
            tryToGetUnstuck();

            return false;
        }

        protected void drawBotPath(Color color)
        {
            Vector3[] botPath = BotOwner.Mover?.GetCurrentPath();
            if (botPath == null)
            {
                LoggingController.LogWarning("Cannot draw null path for " + BotOwner.GetText());
                return;
            }

            //LoggingController.LogInfo("Drawing " + botPath.CalculatePathLength() + "m path with " + botPath.Length + " corners for " + BotOwner.GetText());

            // The visual representation of the bot's path needs to be offset vertically so it's raised above the ground
            List<Vector3> adjustedPathCorners = new List<Vector3>();
            foreach (Vector3 corner in botPath)
            {
                adjustedPathCorners.Add(new Vector3(corner.x, corner.y + 0.75f, corner.z));
            }

            string pathName = "BotPath_" + BotOwner.Id + "_" + DateTime.Now.ToFileTime();

            Models.PathVisualizationData botPathRendering = new Models.PathVisualizationData(pathName, adjustedPathCorners.ToArray(), color);
            Singleton<GameWorld>.Instance.GetComponent<PathRender>().AddOrUpdatePath(botPathRendering);
        }

        protected void outlineTargetPosition(Color color)
        {
            if (!ObjectiveManager.Position.HasValue)
            {
                LoggingController.LogError("Cannot outline null position for bot " + BotOwner.GetText());
                return;
            }

            DebugHelpers.outlinePosition(ObjectiveManager.Position.Value, color);
        }

        private void updateBotStuckDetection()
        {
            if (!lastBotPosition.HasValue)
            {
                lastBotPosition = BotOwner.Position;
            }

            // Check if the bot has moved enough
            float distanceFromLastUpdate = Vector3.Distance(lastBotPosition.Value, BotOwner.Position);
            if (distanceFromLastUpdate > ConfigController.Config.Questing.StuckBotDetection.Distance)
            {
                lastBotPosition = BotOwner.Position;
                restartStuckTimer();
            }
        }

        private void tryToGetUnstuck()
        {
            if (!ConfigController.Config.Questing.StuckBotDetection.StuckBotRemedies.Enabled)
            {
                return;
            }

            // Try jumping
            if
            (
                (StuckTime >= ConfigController.Config.Questing.StuckBotDetection.StuckBotRemedies.MinTimeBeforeJumping)
                && (TimeSinceLastJump > ConfigController.Config.Questing.StuckBotDetection.StuckBotRemedies.JumpDebounceTime)
            )
            {
                LoggingController.LogWarning(BotOwner.GetText() + " is stuck. Trying to jump...");

                tryJump(false);
                timeSinceLastJumpTimer.Restart();
            }

            // Try vaulting
            if
            (
                (StuckTime >= ConfigController.Config.Questing.StuckBotDetection.StuckBotRemedies.MinTimeBeforeVaulting)
                && (TimeSinceLastVault > ConfigController.Config.Questing.StuckBotDetection.StuckBotRemedies.VaultDebounceTime)
            )
            {
                LoggingController.LogWarning(BotOwner.GetText() + " is stuck. Trying to vault...");

                //DelaySprint(5);
                BotOwner.GetPlayer.MovementContext.TryVaulting();
                timeSinceLastVaultTimer.Restart();
            }
        }
    }
}
