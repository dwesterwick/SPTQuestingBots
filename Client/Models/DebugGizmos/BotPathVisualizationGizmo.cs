using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public class BotPathVisualizationGizmo : AbstractDebugGizmo
    {
        private static readonly Vector3 pathOffset = new Vector3(0, 0.5f, 0);

        public Color PathColor { get; private set; }

        protected IBotDebugInfoSource DebugInfoSource;

        private string pathName = null!;
        private Models.Pathing.PathVisualizationData botPathRendering = null!;
        private PathRenderer pathRenderer = null!;

        public BotPathVisualizationGizmo(IBotDebugInfoSource debugInfoSource, Color _pathColor) : base(100)
        {
            DebugInfoSource = debugInfoSource;

            PathColor = _pathColor;

            init();
        }

        private void init()
        {
            pathName = "CurrentBotPath_" + DebugInfoSource.NetId;
            Vector3[] initialPath = getBotPath().ToArray();
            botPathRendering = new Models.Pathing.PathVisualizationData(pathName, initialPath, PathColor);

            pathRenderer = Singleton<GameWorld>.Instance.GetComponent<PathRenderer>();
        }

        public override void Draw() { }

        public override bool ReadyToDispose()
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return false;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(DebugInfoSource.NetId);
            return (botPlayer == null) || !botPlayer.HealthController.IsAlive;
        }

        public override GUIStyle UpdateGUIStyle() => null!;

        protected override void OnDispose() => Disable();

        protected override void OnUpdate()
        {
            if (DebugInfoSource.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotPathVisualizations))
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

        private IEnumerable<Vector3> getBotPath() => DebugInfoSource.CurrentQuestingPath.ApplyOffset(pathOffset);
    }
}
