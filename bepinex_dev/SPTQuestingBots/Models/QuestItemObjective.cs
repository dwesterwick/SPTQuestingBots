using EFT.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class QuestItemObjective : QuestObjective
    {
        public LootItem Item { get; set; } = null;

        public QuestItemObjective() : base()
        {

        }

        public QuestItemObjective(LootItem item, Vector3 position) : base(position)
        {
            Item = item;
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
