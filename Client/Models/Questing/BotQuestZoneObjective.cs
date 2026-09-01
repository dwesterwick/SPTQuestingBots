using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public class BotQuestZoneObjective : BotQuestObjective
    {
        public string ZoneID { get; set; } = null!;

        public BotQuestZoneObjective() : base()
        {

        }

        public BotQuestZoneObjective(string zoneID) : this()
        {
            ZoneID = zoneID;
        }

        public BotQuestZoneObjective(string zoneID, Vector3 position) : base(position)
        {
            ZoneID = zoneID;
        }

        public override void Clear()
        {
            ZoneID = null!;
            base.Clear();
        }

        public override string ToString()
        {
            if (ZoneID != null)
            {
                return "Zone " + (ZoneID ?? "???");
            }

            return base.ToString();
        }
    }
}
