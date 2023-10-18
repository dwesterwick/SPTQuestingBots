using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Models
{
    public class BotBrainType
    {
        private string name;

        public BotBrainType(string _name)
        {
            name = _name;
        }

        public override string ToString() => name;
    }
}
