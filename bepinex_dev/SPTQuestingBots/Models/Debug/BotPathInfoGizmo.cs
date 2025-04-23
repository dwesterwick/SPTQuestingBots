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

namespace SPTQuestingBots.Models.Debug
{
    public class BotPathInfoGizmo : AbstractDebugGizmo
    {
        public DebugMarkerWithOverlay TargetPosition { get; set; }

        private BotOwner bot;
        private BotObjectiveManager botObjectiveManager;

        public bool IsActive => TargetPosition.IsActive;

        public BotPathInfoGizmo(BotOwner _bot, float _markerRadius) : base(100)
        {
            bot = _bot;
            botObjectiveManager = bot.GetObjectiveManager();

            TargetPosition = new DebugMarkerWithOverlay(UpdateGUIStyle, _markerRadius);
        }

        public override bool ReadyToDispose() => (bot == null) || bot.IsDead;

        public override GUIStyle UpdateGUIStyle()
        {
            TargetPosition.Overlay.GuiStyle = DebugHelpers.CreateGuiStyleBotOverlays();
            return TargetPosition.Overlay.GuiStyle;
        }

        public override void Disable() { Destroy(); }

        public void SetActive(bool state)
        {
            TargetPosition.SetActive(state);
        }

        public void Destroy()
        {
            TargetPosition.Destroy();
        }

        protected override void OnUpdate()
        {
            if (!shouldShowOverlay())
            {
                SetActive(false);
                return;
            }

            TargetPosition.Position = botObjectiveManager.BotPath.TargetPosition;
            TargetPosition.Overlay.StaticText = createOverlayText();
            
            updateMarker();

            SetActive(true);
        }

        private bool shouldShowOverlay()
        {
            if (!bot.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotPathOverlays))
            {
                return false;
            }

            // Check if a path has been defined for the bot by this mod
            if (botObjectiveManager?.BotPath?.HasPath != true)
            {
                return false;
            }

            // Ensure the bot is allowed to quest
            if (!botObjectiveManager.IsQuestingAllowed)
            {
                return false;
            }

            return true;
        }

        private string createOverlayText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLabeledValue("Target Position", botObjectiveManager.BotPath.TargetPosition.ToString(), Color.white, Color.white);
            sb.AppendLabeledValue("Bot", bot.GetText(), Color.white, Color.white);
            sb.AppendLabeledValue("Status", botObjectiveManager.BotPath.Status.ToString(), Color.white, botObjectiveManager.BotPath.Status.GetDebugColor());

            return sb.ToString();
        }

        private void updateMarker()
        {
            if (TargetPosition.HasMarker)
            {
                TargetPosition.Marker.transform.position = TargetPosition.Position;
                return;
            }

            TargetPosition.Marker = DebugHelpers.CreateSphere(TargetPosition.Position, TargetPosition.MarkerRadius * 2, Color.green);
        }

        public override void Draw()
        {
            if (!IsActive)
            {
                return;
            }

            TargetPosition.DrawOverlay(TargetPosition.Overlay.StaticText);
        }
    }
}
