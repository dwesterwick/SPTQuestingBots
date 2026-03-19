using SPTarkov.Server.Core.Models.Eft.Bot;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace QuestingBots.Models
{
    public record GenerateBotsWithPScavFlagRequestData : GenerateBotsRequestData
    {
        [JsonPropertyName("GeneratePScav")]
        public bool GeneratePScav { get; set; } = false;

        public GenerateBotsWithPScavFlagRequestData() : base()
        {

        }

        protected override bool PrintMembers(StringBuilder builder)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            builder.Append("GeneratePScav = ");
            builder.Append(GeneratePScav);
            base.PrintMembers(builder);
            return true;
        }
    }
}
