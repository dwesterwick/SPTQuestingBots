using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models
{
    public class QuestItemObjective : QuestObjective
    {
        public LootItem Item { get; set; } = null;

        public QuestItemObjective() : base()
        {

        }

        public QuestItemObjective(LootItem item, Vector3 position) : this()
        {
            Item = item;
            Position = position;
        }

        public override void Clear()
        {
            Item = null;
            base.Clear();
        }

        public override string ToString()
        {
            if (Item != null)
            {
                return "Item " + Item.Item.LocalizedName();
            }

            return base.ToString();
        }
    }
}
