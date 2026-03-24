using QuestingBots.Models;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Eft.Bot;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using System.Text.Json;

namespace QuestingBots.Patches
{
    public class DeserializeGenerateBotsRequestDataPatch : AbstractPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(JsonUtil).GetMethod(nameof(JsonUtil.Deserialize), [typeof(string), typeof(Type)])!;
        }

        [PatchPostfix]
        public static void PatchPostfix(ref object? __result, string? json, Type type)
        {
            if (type != typeof(GenerateBotsRequestData))
            {
                return;
            }

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            __result = JsonSerializer.Deserialize(json, typeof(GenerateBotsWithPScavFlagRequestData), JsonUtil.JsonSerializerOptionsNoIndent);
        }
    }
}
