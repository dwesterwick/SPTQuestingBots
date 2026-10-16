using Comfort.Common;
using EFT;
using EFT.UI.Matchmaker;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using QuestingBots.Models.Questing;
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

        private static object _questingDataLock = new object();
        private IClientBotDebugQuestingInfo? _QuestingData = null;

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

        public void UpdateQuestingBotInfo(IClientBotDebugQuestingInfo data)
        {
            lock (_questingDataLock)
            {
                _QuestingData = data;
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
            bool hasCurrentEnemy;
            int currentEnemyPlayerId;
            bool hasBoss;
            int bossPlayerId;
            lock (_currentDataLock)
            {
                botState = _CurrentData.BotState;
                isSleeping = _CurrentData.IsSleeping;
                botTypeColor = _CurrentData.GetDebugColor();
                activeLayerName = _CurrentData.ActiveLayerName;
                activeLogicName = _CurrentData.ActiveLogicName;
                hasCurrentEnemy = _CurrentData.HasCurrentEnemy;
                currentEnemyPlayerId = _CurrentData.CurrentEnemyPlayerId;
                hasBoss = _CurrentData.HasBoss;
                bossPlayerId = _CurrentData.BossPlayerId;
            }

            sb.AppendLabeledValue(botTypeText, botInfoText, botTypeColor, Color.white);
            sb.AppendLabeledValue("Brain Type", brainNameText, Color.white, Color.white);

            if (botState != EBotState.Active)
            {
                sb.AppendLabeledValue("Bot State", botState.ToString(), Color.white, botState.GetDebugColor());
                return;
            }

            if (botPlayer == null)
            {
                return;
            }

            sb.AppendLabeledValue("Layer", activeLayerName, Color.magenta, Color.magenta);
            sb.AppendLabeledValue("Reason", activeLogicName, Color.white, Color.white);

            if (isSleeping)
            {
                return;
            }

            if (hasCurrentEnemy)
            {
                Player? enemyPlayer = Singleton<PlayerData>.Instance.GetPlayer(currentEnemyPlayerId);
                if (enemyPlayer != null)
                {
                    double enemyDistance = Math.Round(Vector3.Distance(botPlayer.Position, enemyPlayer.Position), 1);
                    sb.AppendLabeledValue("Enemy", $"{enemyPlayer.GetText()}: {enemyDistance}m", Color.white, Color.white);
                }
            }

            if (hasBoss)
            {
                Player? boss = Singleton<PlayerData>.Instance.GetPlayer(bossPlayerId);
                if (boss != null)
                {
                    sb.AppendLabeledValue("Boss", boss.GetText(), Color.white, Color.white);
                }
            }

            if (_QuestingData == null)
            {
                return;
            }

            bool isQuesting;
            string questName;
            string questObjectiveName;
            string questObjectiveStepName;
            EJobAssignmentStatus questStatus;
            EBotQuestingDecision currentDecision;
            UnityEngine.AI.NavMeshPathStatus pathStatus;
            lock (_questingDataLock)
            {
                isQuesting = _QuestingData.IsQuesting;
                questName = _QuestingData.QuestName;
                questObjectiveName = _QuestingData.QuestObjectiveName;
                questObjectiveStepName = _QuestingData.QuestObjectiveStepName;
                questStatus = _QuestingData.QuestStatus;
                currentDecision = _QuestingData.CurrentDecision;
                pathStatus = _QuestingData.PathStatus;
            }

            if (isQuesting)
            {
                sb.AppendLabeledValue("Quest", questName, Color.cyan, Color.cyan);
                sb.AppendLabeledValue("Objective", questObjectiveName, Color.white, Color.white);
                sb.AppendLabeledValue("Step", questObjectiveStepName, Color.white, Color.white);
                sb.AppendLabeledValue("Status", questStatus.ToString(), Color.white, Color.white);
            }

            sb.AppendLabeledValue("Current Decision", currentDecision.ToString(), Color.white, Color.white);

            if (currentDecision.ShouldShowPathStatus() && (pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete))
            {
                sb.AppendLabeledValue("Path Status", pathStatus.ToString(), Color.white, pathStatus.GetDebugColor());
            }
        }

        public override void Draw()
        {
            if ((_CurrentData == null) || (_QuestingData == null))
            {
                return;
            }

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            if (!DebugHelpers.CanDisplayDebugData(_CurrentData, _QuestingData, QuestingBotsPluginConfig.ShowBotInfoOverlays))
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

            Player? myPlayer = Singleton<PlayerData>.Instance.GetMyPlayer();
            if (myPlayer == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot get your position");
                return;
            }

            double distanceToBot = Math.Round(Vector3.Distance(botPlayer.Position, myPlayer.Position), 1);

            Overlay.Draw(addDistanceToStaticText(distanceToBot), botPlayer.Position + new Vector3(0, 1.5f, 0));
        }

        private string addDistanceToStaticText(double distance)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Overlay.StaticText);
            sb.AppendLabeledValue("Distance", distance + "m", Color.white, Color.white);

            return sb.ToString();
        }
    }
}
