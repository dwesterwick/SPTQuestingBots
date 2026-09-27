using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;

namespace QuestingBots.Models.DebugGizmos
{
    public class PlayerCoordinatesGizmo : AbstractDebugTextGizmo
    {
        public PlayerCoordinatesGizmo() : base()
        {
            
        }

        public override bool Enabled => QuestingBotsPluginConfig.ShowCurrentLocation.Value;

        public override int GizmoIndex => 1;

        protected override string GetDebugText()
        {
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return "???";
            }

            return mainPlayer.Position.ToString();
        }
    }
}
