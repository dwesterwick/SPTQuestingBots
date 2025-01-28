using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public class StoredQuestLocation
    {
        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("position")]
        public Vector3 Position { get; private set; }

        public StoredQuestLocation(string name, Vector3 position)
        {
            Name = name;
            Position = position;
        }
    }
}
