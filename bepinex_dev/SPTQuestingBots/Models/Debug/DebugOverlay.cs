using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class DebugOverlay
    {
        public struct GizmoPositionRequestParams
        {
            public Vector2 AdjustedScreenPosition;
            public Vector2 GuiSize;

            public GizmoPositionRequestParams(Vector2 adjustedScreenSize, Vector2 guiSize)
            {
                AdjustedScreenPosition = adjustedScreenSize;
                GuiSize = guiSize;
            }
        }

        public GUIContent GuiContent { get; set; }
        public GUIStyle GuiStyle { get; set; }
        public string StaticText { get; set; } = "";

        private static float _screenScale = 1.0f;
        private static float _nextCheckScreenTime = 0;

        private Func<GUIStyle> getGuiStyle;

        public DebugOverlay(Func<GUIStyle> _getGuiStyle)
        {
            getGuiStyle = _getGuiStyle;
            GuiContent = new GUIContent();
        }

        public DebugOverlay(Func<GUIStyle> _getGuiStyle, string _staticText) : this(_getGuiStyle)
        {
            StaticText = _staticText;
        }

        public void Draw(string text, Func<GizmoPositionRequestParams, Vector2> getGizmoPosition)
        {
            draw_Internal(text, getGizmoPosition, new Vector2(Screen.width, Screen.height));
        }

        public void Draw(string text, Vector3 worldPosition)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
            if (screenPos.z <= 0)
            {
                return;
            }

            draw_Internal(text, getStandardGizmoPosition, new Vector2(screenPos.x, screenPos.y));
        }

        private void draw_Internal(string text, Func<GizmoPositionRequestParams, Vector2> getGizmoPosition, Vector2 screenPosition)
        {
            if (GuiStyle == null)
            {
                GuiStyle = getGuiStyle();
            }

            GuiContent.text = text;
            Vector2 guiSize = GuiStyle.CalcSize(GuiContent);

            float screenScale = getScreenScale();
            Vector2 adjustedScreenSize = new Vector2(screenPosition.x * screenScale, screenPosition.y * screenScale);

            Vector2 gizmoPosition = getGizmoPosition(new GizmoPositionRequestParams(adjustedScreenSize, guiSize));

            Rect rect = new Rect(gizmoPosition, guiSize);
            GUI.Box(rect, GuiContent, GuiStyle);
        }

        // This should be static so it only updates at a fixed interval regardless of how many overlays are created
        private static float getScreenScale()
        {
            if (_nextCheckScreenTime < Time.time && CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                _nextCheckScreenTime = Time.time + 10f;
                _screenScale = (float)CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
            }

            return _screenScale;
        }

        private Vector2 getStandardGizmoPosition(DebugOverlay.GizmoPositionRequestParams requestParams)
        {
            float x = requestParams.AdjustedScreenPosition.x - (requestParams.GuiSize.x / 2);
            float y = Screen.height - (requestParams.AdjustedScreenPosition.y + requestParams.GuiSize.y);

            return new Vector2(x, y);
        }
    }
}
