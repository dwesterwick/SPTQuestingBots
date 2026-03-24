using SPTarkov.Server.Core.Models.Eft.Bot;
using System.Text.Json.Serialization;

namespace QuestingBots.Models
{
    public record GenerateConditionWithPScavFlag : GenerateCondition
    {
        [JsonPropertyName("GeneratePScav")]
        public bool GeneratePScav { get; set; } = false;

        public GenerateConditionWithPScavFlag() : base()
        {

        }

        public GenerateConditionWithPScavFlag(GenerateCondition orignal) : this()
        {
            Role = orignal.Role;
            Difficulty = orignal.Difficulty;
            Limit = orignal.Limit;
        }

        public GenerateConditionWithPScavFlag(GenerateCondition orignal, bool generatePScav) : this(orignal)
        {
            GeneratePScav = generatePScav;
        }
    }
}
