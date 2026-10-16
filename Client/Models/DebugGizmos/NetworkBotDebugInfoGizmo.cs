using Comfort.Common;
using EFT;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static Assets.CommonAssets.Scripts.Utilities.ColliderExtendedDebug;

namespace QuestingBots.Models.DebugGizmos
{
    public class NetworkBotDebugInfoGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; set; }

        private static object _staticDataLock = new object();
        private IClientBotDebugStaticInfo _StaticData;

        private static object _currentDataLock = new object();
        private IClientBotDebugCurrentInfo? _CurrentData = null;

        public int NetId
        {
            get
            {
                lock (_staticDataLock) {  return _StaticData.NetId; }
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

        public override bool ReadyToDispose()
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return false;
            }

            int netId;
            lock (_staticDataLock)
            {
                netId = _StaticData.NetId;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(netId);
            return (botPlayer == null) || !botPlayer.HealthController.IsAlive;
        }

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
            if (_CurrentData == null)
            {
                return;
            }

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            int netId;
            int botId;
            string botTypeText;
            string brainNameText;
            lock (_staticDataLock)
            {
                netId = _StaticData.NetId;
                botId = _StaticData.BotId;
                botTypeText = _StaticData.BotType.ToString();
                brainNameText = _StaticData.BaseBrainName;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(netId);
            string botInfoText = botPlayer.GetText();

            EBotState botState;
            bool isSleeping;
            Color botTypeColor;
            string activeLayerName;
            string activeLogicName;
            lock (_currentDataLock)
            {
                botState = _CurrentData.BotState;
                isSleeping = _CurrentData.IsSleeping;
                botTypeColor = _CurrentData.GetDebugColor();
                activeLayerName = _CurrentData.ActiveLayerName;
                activeLogicName = _CurrentData.ActiveLogicName;
            }

            sb.AppendLabeledValue(botTypeText, botInfoText, botTypeColor, Color.white);
            sb.AppendLabeledValue("Brain Type", brainNameText, Color.white, Color.white);

            if (botState != EBotState.Active)
            {
                sb.AppendLabeledValue("Bot State", botState.ToString(), Color.white, botState.GetDebugColor());
                return;
            }

            sb.AppendLabeledValue("Layer", activeLayerName, Color.magenta, Color.magenta);
            sb.AppendLabeledValue("Reason", activeLogicName, Color.white, Color.white);

            if (isSleeping)
            {
                return;
            }
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

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            int netId;
            lock (_currentDataLock)
            {
                netId = _CurrentData.NetId;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(netId);
            if (botPlayer == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot get position for bot with NetId=" + netId);
                return;
            }

            Vector3 botPosition = botPlayer.Position;

            Overlay.Draw(Overlay.StaticText, botPosition + new Vector3(0, 1.5f, 0));
        }
    }
}
