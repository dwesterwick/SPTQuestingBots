using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public abstract class AbstractDebugTextGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; }

        public AbstractDebugTextGizmo(int updateInterval) : base(updateInterval)
        {
            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public AbstractDebugTextGizmo() : this(100) { }

        public virtual bool Enabled => true;
        public virtual int GizmoIndex => 1;

        public override bool ReadyToDispose() => false;

        protected override void OnDispose()
        {
            Overlay.Dispose();
        }

        protected override void OnUpdate() { }

        public override GUIStyle UpdateGUIStyle()
        {
            Overlay.GuiStyle = DebugHelpers.CreateGuiStyleDebugText();
            return Overlay.GuiStyle;
        }

        public override void Draw()
        {
            if (!Enabled)
            {
                return;
            }

            string text = GetDebugText();
            Overlay.Draw(text, getGizmoPosition);
        }

        protected abstract string GetDebugText();

        private Vector2 getGizmoPosition(DebugOverlay.GizmoPositionRequestParams requestParams)
        {
            float x = requestParams.ScreenPosition.x - requestParams.GuiSize.x - 3;
            float y = requestParams.ScreenPosition.y - ((requestParams.GuiSize.y - 3) * GizmoIndex);

            return new Vector2(x, y);
        }
    }
}
