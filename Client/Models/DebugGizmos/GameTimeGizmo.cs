using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public class GameTimeGizmo : AbstractDebugTextGizmo
    {
        public GameTimeGizmo() : base()
        {

        }

        public override bool Enabled => QuestingBotsPluginConfig.ShowCurrentGameTime.Value;

        public override int GizmoIndex => 2;

        protected override string GetDebugText()
        {
            DateTime gameDateTime = RaidHelpers.GetGameDateTime();
            return gameDateTime.ToString("HH:mm");
        }
    }
}
