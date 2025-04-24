using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models.Questing;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class JobAssignmentGizmo : AbstractDebugGizmo
    {
        private static readonly float overlayHeightOffset = 0.1f;

        public DebugMarkerWithOverlay MarkerAndOverlay { get; set; }
        public double Distance { get; set; } = double.PositiveInfinity;
        public Color MarkerColor { get; private set; }

        private JobAssignment jobAssignment;

        public bool IsActive => MarkerAndOverlay.IsActive;

        public JobAssignmentGizmo(JobAssignment _jobAssignment, Vector3 _position, string _staticText, float _markerRadius, Color _markerColor) : base(200)
        {
            jobAssignment = _jobAssignment;
            
            MarkerAndOverlay = new DebugMarkerWithOverlay(UpdateGUIStyle, _markerRadius);
            MarkerAndOverlay.Overlay.StaticText = _staticText;

            
            Vector3 overlayPosition = _position + new Vector3(0, _markerRadius + overlayHeightOffset, 0);
            MarkerAndOverlay.Position = overlayPosition;

            MarkerColor = _markerColor;
            MarkerAndOverlay.Marker = DebugHelpers.CreateSphere(_position, _markerRadius * 2, MarkerColor);
        }

        public override bool ReadyToDispose() => false;

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
            updateDistance();
            updateVisibility();
        }

        private void updateVisibility()
        {
            bool isVisible = QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value;
            isVisible &= Distance <= QuestingBotsPluginConfig.QuestOverlayMaxDistance.Value;
            isVisible &= QuestingBotsPluginConfig.ShowQuestInfoForSpawnSearchQuests.Value || !jobAssignment.IsSpawnSearchQuest;

            SetActive(isVisible);
        }

        private void updateDistance()
        {
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;

            Distance = (mainPlayer == null) ? double.PositiveInfinity : Math.Round(Vector3.Distance(mainPlayer.Position, jobAssignment.Position.Value), 1);
        }

        public override void Draw()
        {
            if (!IsActive)
            {
                return;
            }

            string text = MarkerAndOverlay.Overlay.StaticText + Distance;
            MarkerAndOverlay.DrawOverlay(text);
        }
    }
}
