using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.Models
{
    public class BotBrainType
    {
        public WildSpawnType SpawnType { get; private set; } = WildSpawnType.test;
        public string Name { get; private set; } = "???";

        public BotBrainType(string _name)
        {
            Name = _name;
        }

        public BotBrainType(string _name, WildSpawnType _spawnType) : this(_name)
        {
            SpawnType = _spawnType;
        }

        public override string ToString() => Name;
    }
}
