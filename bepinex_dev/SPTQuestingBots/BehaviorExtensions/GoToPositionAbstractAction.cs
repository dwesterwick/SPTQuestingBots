using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.BotLogic;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BehaviorExtensions
{
    public abstract class GoToPositionAbstractAction : CustomLogicDelayedUpdate
    {
        private const int BRAIN_LAYER_ERROR_MESSAGE_INTERVAL = 30;

        protected bool CanSprint { get; set; } = true;

        private static FieldInfo botZoneField = null;

        private Stopwatch botIsStuckTimer = new Stopwatch();
        private Stopwatch timeSinceLastJumpTimer = Stopwatch.StartNew();
        private Stopwatch timeSinceLastVaultTimer = Stopwatch.StartNew();
        private Stopwatch timeSinceLastBrainLayerMessageTimer = Stopwatch.StartNew();
        private Vector3? lastBotPosition = null;
        private bool loggedBrainLayerError = false;

        protected double StuckTime => botIsStuckTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSinceLastJump => timeSinceLastJumpTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSinceLastVault => timeSinceLastVaultTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSinceLastBrainLayerMessage => timeSinceLastBrainLayerMessageTimer.ElapsedMilliseconds / 1000.0;

        public GoToPositionAbstractAction(BotOwner _BotOwner, int delayInterval) : base(_BotOwner, delayInterval)
        {
            if (botZoneField == null)
            {
                botZoneField = AccessTools.Field(typeof(BotsGroup), "<BotZone>k__BackingField");
            }
        }

        public GoToPositionAbstractAction(BotOwner _BotOwner) : this(_BotOwner, updateInterval)
        {

        }

        public override void Start()
        {
            base.Start();

            resumeStuckTimer();

            timeSinceLastJumpTimer.Restart();
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            base.Stop();

            pauseStuckTimer();

            BotOwner.PatrollingData.Unpause();

            updateBotZoneForGroup();
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position)
        {
            return RecalculatePath(position, 0.2f, 0.5f);
        }

        public NavMeshPathStatus? RecalculatePath(Vector3 position, float targetVariationAllowed, float reachDist, bool force = false)
        {
            // If a bot is jumping or vaulting, recalculate its path after it finishes
            if (BotOwner.GetPlayer.MovementContext.PlayerAnimatorIsJumpSetted() || BotOwner.GetPlayer.MovementContext.PlayerAnimatorGetIsVaulting())
            {
                ObjectiveManager.BotPath.ForcePathRecalculation();
                return ObjectiveManager.BotPath.Status;
            }

            if (!isAQuestingBotsBrainLayerActive())
            {
                return ObjectiveManager.BotPath.Status;
            }

            Models.Pathing.BotPathUpdateNeededReason updateReason = ObjectiveManager.BotPath.CheckIfUpdateIsNeeded(position, targetVariationAllowed, reachDist, force);

            if (ObjectiveManager.BotPath.Status != NavMeshPathStatus.PathInvalid)
            {
                if (updateReason != Models.Pathing.BotPathUpdateNeededReason.None)
                {
                    BotOwner.FollowPath(ObjectiveManager.BotPath, true, false);
                }
            }
            else
            {
                BotOwner.Mover?.Stop();
            }

            return ObjectiveManager.BotPath.Status;
        }

        private bool isAQuestingBotsBrainLayerActive()
        {
            string activeLayerName = BotOwner.Brain.ActiveLayerName();
            if (LogicLayerMonitor.QuestingBotsBrainLayerNames.Contains(activeLayerName))
            {
                loggedBrainLayerError = false;
                return true;
            }

            if (!loggedBrainLayerError || (TimeSinceLastBrainLayerMessage >= BRAIN_LAYER_ERROR_MESSAGE_INTERVAL))
            {
                LoggingController.LogError("Cannot recalculate path for " + BotOwner.GetText() + " because the active brain layer is not a Questing Bots layer. This is normally caused by an exception in the update logic of another layer. Active layer name: " + activeLayerName);

                loggedBrainLayerError = true;
                timeSinceLastBrainLayerMessageTimer.Restart();
            }

            return false;
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

        protected void pauseStuckTimer()
        {
            botIsStuckTimer.Stop();
        }

        protected void resumeStuckTimer()
        {
            botIsStuckTimer.Start();
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

            Models.Pathing.PathVisualizationData botPathRendering = new Models.Pathing.PathVisualizationData(pathName, adjustedPathCorners.ToArray(), color);
            Singleton<GameWorld>.Instance.GetComponent<PathRenderer>().AddOrUpdatePath(botPathRendering);
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

        protected void updateBotZoneForGroup(bool allowForFollowers = false)
        {
            if (!ConfigController.Config.Questing.UpdateBotZoneAfterStopping)
            {
                return;
            }

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            BotZone closestBotZone = botSpawnerClass.GetClosestZone(BotOwner.Position, out float dist);

            if (BotOwner.BotsGroup.BotZone == closestBotZone)
            {
                return;
            }

            // Do not allow followers to set the BotZone
            if (!allowForFollowers && !BotOwner.Boss.IamBoss && (BotOwner.BotsGroup.MembersCount > 1))
            {
                return;
            }

            //Controllers.LoggingController.LogWarning("Changing BotZone for group containing " + BotOwner.GetText() + " from " + BotOwner.BotsGroup.BotZone.ShortName + " to " + closestBotZone.ShortName + "...");

            botZoneField.SetValue(BotOwner.BotsGroup, closestBotZone);
            BotOwner.PatrollingData.PointChooser.ShallChangeWay(true);
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
                if (!canUseStuckRemedies())
                {
                    return;
                }

                LoggingController.LogWarning(BotOwner.GetText() + " is stuck. Trying to jump...");

                BotOwner.Mover.Stop();
                BotOwner.Mover.SetPose(1f);
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
                if (!canUseStuckRemedies())
                {
                    return;
                }

                LoggingController.LogWarning(BotOwner.GetText() + " is stuck. Trying to vault...");

                //DelaySprint(5);
                BotOwner.Mover.Stop();
                BotOwner.Mover.SetPose(1f);
                BotOwner.GetPlayer.MovementContext.TryVaulting();
                timeSinceLastVaultTimer.Restart();
            }
        }

        private bool canUseStuckRemedies()
        {
            //LoggingController.LogWarning(BotOwner.GetText() + " was stuck for " + StuckTime + "s.");

            if (!BotOwner.GetPlayer.MovementContext.IsGrounded)
            {
                LoggingController.LogWarning(BotOwner.GetText() + " is stuck, but countermeasures are unavailable until its grounded.");
                return false;
            }

            return true;
        }
    }
}
