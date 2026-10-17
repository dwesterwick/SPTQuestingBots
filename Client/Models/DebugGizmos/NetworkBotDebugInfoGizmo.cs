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

namespace QuestingBots.Models.DebugGizmos
{
    public class NetworkBotDebugInfoGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; set; }

        private IBotDebugInfoSource _debugInfoSource;

        public int BotNetId => _debugInfoSource.NetId;

        public NetworkBotDebugInfoGizmo(IBotDebugInfoSource debugInfoSource) : base(100)
        {
            _debugInfoSource = debugInfoSource;
            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public override bool ReadyToDispose()
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return false;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(_debugInfoSource.NetId);
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
            if (!_debugInfoSource.HasCurrentData)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            updateOverlayText(sb);
            Overlay.StaticText = sb.ToString();
        }

        private void updateOverlayText(StringBuilder sb)
        {
            if (!_debugInfoSource.HasCurrentData)
            {
                return;
            }

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(_debugInfoSource.NetId);
            string botInfoText = botPlayer.GetText();

            sb.AppendLabeledValue(_debugInfoSource.BotType.ToString(), botInfoText, _debugInfoSource.GetDebugColor(), Color.white);
            sb.AppendLabeledValue("Brain Type", _debugInfoSource.BaseBrainName, Color.white, Color.white);

            if (_debugInfoSource.BotState != EBotState.Active)
            {
                sb.AppendLabeledValue("Bot State", _debugInfoSource.BotState.ToString(), Color.white, _debugInfoSource.BotState.GetDebugColor());
                return;
            }

            if (botPlayer == null)
            {
                return;
            }

            sb.AppendLabeledValue("Layer", _debugInfoSource.ActiveLayerName, Color.magenta, Color.magenta);
            sb.AppendLabeledValue("Reason", _debugInfoSource.ActiveLogicName, Color.white, Color.white);

            if (_debugInfoSource.IsSleeping)
            {
                return;
            }

            if (_debugInfoSource.HasCurrentEnemy)
            {
                Player? enemyPlayer = Singleton<PlayerData>.Instance.GetPlayer(_debugInfoSource.CurrentEnemyPlayerId);
                if (enemyPlayer != null)
                {
                    double enemyDistance = Math.Round(Vector3.Distance(botPlayer.Position, enemyPlayer.Position), 1);
                    sb.AppendLabeledValue("Enemy", $"{enemyPlayer.GetText()}: {enemyDistance}m", Color.white, Color.white);
                }
            }

            if (_debugInfoSource.HasBoss)
            {
                Player? boss = Singleton<PlayerData>.Instance.GetPlayer(_debugInfoSource.BossPlayerId);
                if (boss != null)
                {
                    sb.AppendLabeledValue("Boss", boss.GetText(), Color.white, Color.white);
                }
            }

            if (!_debugInfoSource.HasQuestingData || !_debugInfoSource.AllowedToQuest)
            {
                return;
            }

            if (_debugInfoSource.IsQuesting)
            {
                sb.AppendLabeledValue("Quest", _debugInfoSource.QuestName, Color.cyan, Color.cyan);
                sb.AppendLabeledValue("Objective", _debugInfoSource.QuestObjectiveName, Color.white, Color.white);
                sb.AppendLabeledValue("Step", _debugInfoSource.QuestObjectiveStepName, Color.white, Color.white);
                sb.AppendLabeledValue("Status", _debugInfoSource.QuestStatus.ToString(), Color.white, Color.white);
            }

            sb.AppendLabeledValue("Current Decision", _debugInfoSource.CurrentDecision.ToString(), Color.white, Color.white);

            if (_debugInfoSource.CurrentDecision.ShouldShowPathStatus() && (_debugInfoSource.PathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete))
            {
                sb.AppendLabeledValue("Path Status", _debugInfoSource.PathStatus.ToString(), Color.white, _debugInfoSource.PathStatus.GetDebugColor());
            }
        }

        public override void Draw()
        {
            if (!_debugInfoSource.HasCurrentData || !_debugInfoSource.HasQuestingData)
            {
                return;
            }

            if (!Singleton<PlayerData>.Instantiated)
            {
                return;
            }

            if (!_debugInfoSource.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotInfoOverlays))
            {
                return;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(_debugInfoSource.NetId);
            if (botPlayer == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot get position for bot with NetId=" + _debugInfoSource.NetId);
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
