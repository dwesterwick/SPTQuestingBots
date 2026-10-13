using EFT;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public class NetworkBotDebugInfoGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; set; }

        private static object _staticDataLock = new object();
        private IClientBotDebugStaticInfo _StaticData;

        private static object _currentDataLock = new object();
        private IClientBotDebugCurrentInfo? _CurrentData = null;

        public int BotId
        {
            get
            {
                lock (_staticDataLock) {  return _StaticData.BotId; }
            }
        }

        public NetworkBotDebugInfoGizmo(IClientBotDebugStaticInfo data) : base(100)
        {
            lock (_staticDataLock)
            {
                _StaticData = data;
            }

            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public void UpdateStaticBotInfo(IClientBotDebugStaticInfo data)
        {
            lock (_staticDataLock)
            {
                _StaticData = data;
            }
        }

        public void UpdateCurrentBotInfo(IClientBotDebugCurrentInfo data)
        {
            lock (_currentDataLock)
            {
                _CurrentData = data;
            }
        }

        public override bool ReadyToDispose() => _CurrentData?.IsDead == true;

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
            if (_CurrentData == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            updateOverlayText(sb);
            Overlay.StaticText = sb.ToString();
        }

        private void updateOverlayText(StringBuilder sb)
        {
            string botTypeText;
            string nickname;
            int id;
            int level;
            lock (_staticDataLock)
            {
                botTypeText = _StaticData.BotType.ToString();
                nickname = _StaticData.CorrectedNickname;
                id = _StaticData.BotId;
                level = _StaticData.Level;
            }

            sb.AppendLabeledValue(botTypeText, $"{nickname} (Name: Bot{id}, Level: {level})", Color.white, Color.white);
        }

        public override void Draw()
        {
            if (_CurrentData == null)
            {
                return;
            }

            if (!_StaticData.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotInfoOverlays))
            {
                return;
            }

            Vector3 botHeadPosition;
            lock (_currentDataLock)
            {
                botHeadPosition = _CurrentData.Position + new Vector3(0, 1.5f, 0);
            }
            
            Overlay.Draw(Overlay.StaticText, botHeadPosition);
        }
    }
}
