using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Models.Debug
{
    public abstract class AbstractBotPathMarkerGizmo : AbstractDebugGizmo
    {
        public DebugMarkerWithOverlay MarkerAndOverlay { get; set; }
        public string MarkerName { get; private set; }
        public Color MarkerColor { get; private set; }
        
        protected BotOwner BotOwner;
        protected BotObjectiveManager BotObjectiveManager;

        public bool IsActive => MarkerAndOverlay.IsActive;
        public float MarkerRadius => MarkerAndOverlay.MarkerRadius;

        public AbstractBotPathMarkerGizmo(BotOwner _bot, float _markerRadius, string _markerName, Color _color) : base(100)
        {
            MarkerName = _markerName;
            MarkerColor = _color;

            BotOwner = _bot;
            BotObjectiveManager = BotOwner.GetObjectiveManager();

            GameObject marker = DebugHelpers.CreateSphere(Vector3.negativeInfinity, _markerRadius * 2, MarkerColor);
            DebugOverlay overlay = new DebugOverlay(UpdateGUIStyle);
            MarkerAndOverlay = new DebugMarkerWithOverlay(UpdateGUIStyle, _markerRadius, marker, overlay);
        }

        protected abstract bool IsEnabled();
        protected abstract bool HasValidPath();
        protected abstract Vector3 GetPosition();
        protected abstract NavMeshPathStatus? GetPathStatus();
        
        public override bool ReadyToDispose() => (BotOwner == null) || BotOwner.IsDead;

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

            MarkerAndOverlay.Position = GetPosition();
            MarkerAndOverlay.Marker.transform.position = MarkerAndOverlay.Position;
            MarkerAndOverlay.Overlay.StaticText = createOverlayText(MarkerName, MarkerAndOverlay.Position, GetPathStatus());
            
            SetActive(true);
        }

        private bool shouldShow()
        {
            if (!BotOwner.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotPathOverlays))
            {
                return false;
            }

            return HasValidPath();
        }

        private string createOverlayText(string markerName, Vector3 markerPosition, NavMeshPathStatus? pathStatus)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLabeledValue(markerName, markerPosition.ToString(), Color.white, Color.white);
            sb.AppendLabeledValue("Bot", BotOwner.GetText(), Color.white, Color.white);

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
