using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class PlayerCoordinatesGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; }

        public PlayerCoordinatesGizmo() : base(100)
        {
            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public override bool ReadyToDispose() => false;
        public override void Disable() { }
        protected override void OnUpdate() { }

        public override GUIStyle UpdateGUIStyle()
        {
            Overlay.GuiStyle = DebugHelpers.CreateGuiStylePlayerCoordinates();
            return Overlay.GuiStyle;
        }

        public override void Draw()
        {
            if (!QuestingBotsPluginConfig.ShowCurrentLocation.Value)
            {
                return;
            }

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return;
            }

            string text = mainPlayer.Position.ToString();
            Overlay.Draw(text, getGizmoPosition);
        }

        private Vector2 getGizmoPosition(DebugOverlay.GizmoPositionRequestParams requestParams)
        {
            float x = requestParams.AdjustedScreenPosition.x - requestParams.GuiSize.x - 3;
            float y = requestParams.AdjustedScreenPosition.y - requestParams.GuiSize.y - 3;

            return new Vector2(x, y);
        }
    }
}
