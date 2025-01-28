using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public class QuestZoneObjective : QuestObjective
    {
        public string ZoneID { get; set; } = null;

        public QuestZoneObjective() : base()
        {

        }

        public QuestZoneObjective(string zoneID) : this()
        {
            ZoneID = zoneID;
        }

        public QuestZoneObjective(string zoneID, Vector3 position) : base(position)
        {
            ZoneID = zoneID;
        }

        public override void Clear()
        {
            ZoneID = null;
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
