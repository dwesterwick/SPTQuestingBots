using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Interactive;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public class QuestItemObjective : QuestObjective
    {
        public LootItem Item { get; set; } = null;

        private string ItemName = null;

        public QuestItemObjective() : base()
        {

        }

        public QuestItemObjective(LootItem item, Vector3 position) : base(position)
        {
            Item = item;
            ItemName = Item.Item.LocalizedName();
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
                return "Item " + (ItemName ?? "???");
            }

            return base.ToString();
        }
    }
}
