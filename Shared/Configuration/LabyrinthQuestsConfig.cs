using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LabyrinthQuestsConfig
    {
        [DataMember(Name = "max_collider_magnitude_to_block_navmesh", IsRequired = true)]
        public float MaxColliderMagnitudeToBlockNavmesh { get; set; } = 10;

        [DataMember(Name = "pmc_bots_trigger_alarms", IsRequired = true)]
        public bool PMCBotsTriggerAlarms { get; set; } = true;

        [DataMember(Name = "block_navmesh_for_starting_chamber_traps", IsRequired = true)]
        public bool BlockNavmeshForStartingChamberTraps { get; set; } = true;

        [DataMember(Name = "block_navmesh_for_intoxication_traps", IsRequired = true)]
        public bool BlockNavmeshForIntoxicationTraps { get; set; } = true;

        public LabyrinthQuestsConfig()
        {

        }
    }
}
