using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.BotMonitor;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models.Questing;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class BotInfoGizmo : AbstractDebugGizmo
    {
        public DebugOverlay Overlay { get; set; }

        private BotOwner bot;
        private BotObjectiveManager botObjectiveManager;
        private BotQuestingDecisionMonitor botQuestingDecisionMonitor;

        public BotInfoGizmo(BotOwner _bot) : base(100)
        {
            bot = _bot;
            Overlay = new DebugOverlay(UpdateGUIStyle);
        }

        public override bool ReadyToDispose() => (bot == null) || bot.IsDead;

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
            if (!bot.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotInfoOverlays))
            {
                return;
            }

            if (botObjectiveManager == null)
            {
                botObjectiveManager = bot.GetObjectiveManager();
            }
            if (botQuestingDecisionMonitor == null)
            {
                botQuestingDecisionMonitor = botObjectiveManager?.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
            }

            StringBuilder sb = new StringBuilder();
            updateOverlayText(sb);
            Overlay.StaticText = sb.ToString();
        }

        private void updateOverlayText(StringBuilder sb)
        {
            sb.AppendLabeledValue(Controllers.BotRegistrationManager.GetBotType(bot).ToString(), bot.GetText(), bot.GetDebugColor(), Color.white);
            sb.AppendLabeledValue("Brain Type", bot.Brain.BaseBrain.ShortName(), Color.white, Color.white);

            if (bot.BotState != EBotState.Active)
            {
                sb.AppendLabeledValue("Bot State", bot.BotState.ToString(), Color.white, bot.BotState.GetDebugColor());
                return;
            }

            sb.AppendLabeledValue("Layer", bot.GetActiveLayerName(), Color.magenta, Color.magenta);
            sb.AppendLabeledValue("Reason", bot.GetActiveLogicName(), Color.white, Color.white);

            if (botObjectiveManager == null)
            {
                return;
            }

            if (BotRegistrationManager.IsBotSleeping(bot.Profile.Id))
            {
                return;
            }

            BotOwner boss = BotHiveMindMonitor.GetBoss(bot);
            if (boss != null)
            {
                sb.AppendLabeledValue("Boss", boss.GetText(), Color.white, boss.IsDead ? Color.red : Color.white);
            }
            else if (botObjectiveManager.IsQuestingAllowed)
            {
                BotJobAssignment botJobAssignment = BotJobAssignmentFactory.GetCurrentJobAssignment(bot, false);
                if (botJobAssignment != null)
                {
                    sb.AppendLabeledValue("Quest", botJobAssignment.QuestAssignment?.ToString(), Color.cyan, Color.cyan);
                    sb.AppendLabeledValue("Objective", botJobAssignment.QuestObjectiveAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Step", botJobAssignment.QuestObjectiveStepAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Status", botJobAssignment.Status.ToString(), Color.white, Color.white);
                }
            }

            if ((botQuestingDecisionMonitor == null) || !botObjectiveManager.IsQuestingAllowed)
            {
                return;
            }

            sb.AppendLabeledValue("Current Decision", botQuestingDecisionMonitor.CurrentDecision.ToString(), Color.white, Color.white);

            if (shouldShowPathStatus(botQuestingDecisionMonitor.CurrentDecision) && (botObjectiveManager.BotPath != null))
            {
                sb.AppendLabeledValue("Path Status", botObjectiveManager.BotPath.Status.ToString(), Color.white, botObjectiveManager.BotPath.Status.GetDebugColor());
            }
        }

        private bool shouldShowPathStatus(BotQuestingDecision decision)
        {
            switch (decision)
            {
                case BotQuestingDecision.Quest:
                case BotQuestingDecision.Regroup:
                case BotQuestingDecision.FollowBoss:
                case BotQuestingDecision.HelpBoss:
                    return true;
            }

            return false;
        }

        public override void Draw()
        {
            if (!bot.CanDisplayDebugData(QuestingBotsPluginConfig.ShowBotInfoOverlays))
            {
                return;
            }

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return;
            }

            Vector3 botHeadPosition = bot.Position + new Vector3(0, 1.5f, 0);
            Overlay.Draw(addDistanceToStaticText(mainPlayer.Position), botHeadPosition);
        }

        private string addDistanceToStaticText(Vector3 sourcePosition)
        {
            double distanceToBot = Math.Round(Vector3.Distance(bot.Position, sourcePosition), 1);

            StringBuilder sb = new StringBuilder();
            sb.Append(Overlay.StaticText);
            sb.AppendLabeledValue("Distance", distanceToBot + "m", Color.white, Color.white);

            return sb.ToString();
        }
    }
}
