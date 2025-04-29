using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class BotPathVisualizationGizmo : AbstractDebugGizmo
    {
        private static readonly Vector3 pathOffset = new Vector3(0, 0.5f, 0);

        public Color PathColor { get; private set; }

        protected BotOwner BotOwner;

        private string pathName;
        private Models.Pathing.PathVisualizationData botPathRendering;
        private PathRenderer pathRenderer;

        public BotPathVisualizationGizmo(BotOwner _botOwner, Color _pathColor) : base(100)
        {
            BotOwner = _botOwner;

            PathColor = _pathColor;

            init();
        }

        private void init()
        {
            pathName = "CurrentBotPath_" + BotOwner.Id;
            Vector3[] initialPath = getBotPath().ToArray();
            botPathRendering = new Models.Pathing.PathVisualizationData(pathName, initialPath, PathColor);

            pathRenderer = Singleton<GameWorld>.Instance.GetComponent<PathRenderer>();
        }

        public override void Draw() { }

        public override bool ReadyToDispose() => (BotOwner == null) || BotOwner.IsDead;

        public override GUIStyle UpdateGUIStyle() => null;

        protected override void OnDispose() => Disable();

        protected override void OnUpdate()
        {
            if (BotOwner.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotPathVisualizations))
            {
                botPathRendering.PathData = getBotPath();
                pathRenderer.AddOrUpdatePath(botPathRendering, false);
            }
            else
            {
                Disable();
            }
        }

        public void Disable()
        {
            pathRenderer.RemovePath(pathName);
        }

        private IEnumerable<Vector3> getBotPath() => BotOwner.Mover
            .GetCurrentPath()
            .ApplyOffset(pathOffset);
    }
}
