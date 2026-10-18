using Comfort.Common;
using EFT;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Models.DebugGizmos
{
    public abstract class AbstractBotPathMarkerGizmo : AbstractDebugGizmo
    {
        public DebugMarkerWithOverlay MarkerAndOverlay { get; set; }
        public string MarkerName { get; private set; }
        public Color MarkerColor { get; private set; }
        
        protected IBotDebugInfoSource DebugInfoSource;

        public bool IsActive => MarkerAndOverlay.IsActive;
        public float MarkerRadius => MarkerAndOverlay.MarkerRadius;

        public AbstractBotPathMarkerGizmo(IBotDebugInfoSource debugInfoSource, float _markerRadius, string _markerName, Color _color) : base(100)
        {
            MarkerName = _markerName;
            MarkerColor = _color;

            DebugInfoSource = debugInfoSource;

            GameObject marker = DebugHelpers.CreateSphere(Vector3.negativeInfinity, _markerRadius * 2, MarkerColor);
            DebugOverlay overlay = new DebugOverlay(UpdateGUIStyle);
            MarkerAndOverlay = new DebugMarkerWithOverlay(UpdateGUIStyle, _markerRadius, marker, overlay);
        }

        protected abstract bool IsEnabled();
        protected abstract bool HasValidPath();
        protected abstract Vector3? GetPosition();
        protected abstract NavMeshPathStatus? GetPathStatus();

        public override bool ReadyToDispose()
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return false;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(DebugInfoSource.NetId);
            return (botPlayer == null) || !botPlayer.HealthController.IsAlive;
        }

        public override GUIStyle UpdateGUIStyle()
        {
            MarkerAndOverlay.Overlay.GuiStyle = DebugHelpers.CreateGuiStyleBotOverlays();
            return MarkerAndOverlay.Overlay.GuiStyle;
        }

        protected override void OnDispose()
        {
            Destroy();
        }

        public void SetActive(bool state)
        {
            MarkerAndOverlay.SetActive(state);
        }

        public void Destroy()
        {
            MarkerAndOverlay.Destroy();
        }

        protected override void OnUpdate()
        {
            if (!IsEnabled() || !shouldShow())
            {
                SetActive(false);
                return;
            }

            Vector3? position = GetPosition();
            if (position == null)
            {
                return;
            }

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            MarkerAndOverlay.Position = position.Value;
            MarkerAndOverlay.Marker.transform.position = MarkerAndOverlay.Position;
            MarkerAndOverlay.Overlay.StaticText = createOverlayText(MarkerName, MarkerAndOverlay.Position, GetPathStatus());
            
            SetActive(true);
        }

        private bool shouldShow()
        {
            if (!DebugInfoSource.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotPathOverlays))
            {
                return false;
            }

            return HasValidPath();
        }

        private string createOverlayText(string markerName, Vector3 markerPosition, NavMeshPathStatus? pathStatus)
        {
            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(DebugInfoSource.NetId);

            StringBuilder sb = new StringBuilder();
            sb.AppendLabeledValue(markerName, markerPosition.ToString(), Color.white, Color.white);
            sb.AppendLabeledValue("Bot", botPlayer.GetText(), Color.white, Color.white);

            if (pathStatus.HasValue)
            {
                sb.AppendLabeledValue("Status", pathStatus.ToString(), Color.white, pathStatus.Value.GetDebugColor());
            }

            return sb.ToString();
        }

        public override void Draw()
        {
            if (!IsActive)
            {
                return;
            }

            MarkerAndOverlay.DrawOverlay(MarkerAndOverlay.Overlay.StaticText);
        }
    }
}
