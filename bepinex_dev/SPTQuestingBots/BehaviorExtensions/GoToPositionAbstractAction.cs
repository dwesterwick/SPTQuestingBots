using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.BehaviorExtensions
{
    public abstract class GoToPositionAbstractAction : CustomLogicDelayedUpdate
    {
        protected bool CanSprint { get; set; } = true;

        private Stopwatch botIsStuckTimer = new Stopwatch();
        private Vector3? lastBotPosition = null;

        protected double StuckTime => botIsStuckTimer.ElapsedMilliseconds / 1000.0;

        public GoToPositionAbstractAction(BotOwner _BotOwner, int delayInterval) : base(_BotOwner, delayInterval)
        {
            
        }

        public GoToPositionAbstractAction(BotOwner _BotOwner) : this(_BotOwner, updateInterval)
        {

        }

        public override void Start()
        {
            base.Start();
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

        public NavMeshPathStatus? RecalculatePath(Vector3 position, float reachDist)
        {
            // Recalculate a path to the bot's objective. This should be done cyclically in case locked doors are opened, etc. 
            return BotOwner.Mover?.GoToPoint(position, true, reachDist, false, false);
        }

        protected void restartStuckTimer()
        {
            botIsStuckTimer.Restart();
        }

        protected bool checkIfBotIsStuck()
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

            // If the bot hasn't moved enough within a certain time while this layer is active, assume the bot is stuck
            if (StuckTime > ConfigController.Config.Questing.StuckBotDetection.Time)
            {
                if (ConfigController.Config.Debug.ShowFailedPaths)
                {
                    drawBotPath(Color.red);
                }

                return true;
            }

            return false;
        }

        protected void drawBotPath(Color color)
        {
            Vector3[] botPath = BotOwner.Mover?.CurPath;
            if (botPath == null)
            {
                return;
            }

            List<Vector3> adjustedPathCorners = new List<Vector3>();
            foreach (Vector3 corner in botPath)
            {
                adjustedPathCorners.Add(new Vector3(corner.x, corner.y + 0.75f, corner.z));
            }

            string pathName = "BotPath_" + BotOwner.Id + "_" + DateTime.Now.ToFileTime();
            PathRender.RemovePath(pathName);
            Models.PathVisualizationData botPathRendering = new Models.PathVisualizationData(pathName, adjustedPathCorners.ToArray(), color);
            PathRender.AddOrUpdatePath(botPathRendering);
        }
    }
}
