using EFT;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public class NetworkBotDebugInfoGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; set; }

        private INetworkBotDebugInfo _BotData;

        public NetworkBotDebugInfoGizmo(INetworkBotDebugInfo data) : base(100)
        {
            _BotData = data;
            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public void UpdateNetworkBotInfo(INetworkBotDebugInfo data)
        {
            _BotData = data;
        }

        public override bool ReadyToDispose() => _BotData.IsDead;

        public override GUIStyle UpdateGUIStyle()
        {
            Overlay.GuiStyle = DebugHelpers.CreateGuiStyleBotOverlays();
            return Overlay.GuiStyle;
        }

        protected override void OnDispose()
        {
            Overlay.Dispose();
        }

        protected override void OnUpdate()
        {
            StringBuilder sb = new StringBuilder();
            updateOverlayText(sb);
            Overlay.StaticText = sb.ToString();
        }

        private void updateOverlayText(StringBuilder sb)
        {
            sb.AppendLabeledValue("Bot", _BotData.BotId.ToString(), Color.white, Color.white);
        }

        public override void Draw()
        {
            if (!_BotData.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotInfoOverlays))
            {
                return;
            }

            Vector3 botHeadPosition = _BotData.Position + new Vector3(0, 1.5f, 0);
            Overlay.Draw(Overlay.StaticText, botHeadPosition);
        }
    }
}
