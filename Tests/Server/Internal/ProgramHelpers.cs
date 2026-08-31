using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuestingBots.Server.Internal;

// Copied from https://github.com/sp-tarkov/server-csharp/blob/main/SPTarkov.Server/Helpers/ProgramHelpers.cs

public static class ProgramHelpers
{
    private static readonly JsonSerializerOptions _earlyLocaleJsonSerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static LocaleTable CreateEarlyLocaleTable()
    {
        var localesPath = Path.Combine(".", "SPT_Data", "database", "locales");
        var globalPath = Path.Combine(localesPath, "global");
        var menuPath = Path.Combine(localesPath, "menu");
        var languagesPath = Path.Combine(localesPath, "languages.json");

        if (!Directory.Exists(globalPath) || !Directory.Exists(menuPath) || !File.Exists(languagesPath))
        {
            throw new InvalidOperationException($"Unable to load early locale table from '{Path.GetFullPath(localesPath)}'.");
        }

        return new LocaleTable
        {
            Global = Directory
                .EnumerateFiles(globalPath, "*.json")
                .ToDictionary(
                    GetLocaleKey,
                    file => new LazyLoad<GlobalLocaleDictionary>(() => DeserializeFromFile<GlobalLocaleDictionary>(file) ?? []),
                    StringComparer.OrdinalIgnoreCase
                ),
            Menu = Directory
                .EnumerateFiles(menuPath, "*.json")
                .ToDictionary(
                    GetLocaleKey,
                    file => DeserializeFromFile<Dictionary<string, object>>(file) ?? [],
                    StringComparer.OrdinalIgnoreCase
                ),
            Languages = DeserializeFromFile<Dictionary<string, string>>(languagesPath) ?? [],
        };
    }

    private static T? DeserializeFromFile<T>(string file)
    {
        using var stream = File.OpenRead(file);
        return JsonSerializer.Deserialize<T>(stream, _earlyLocaleJsonSerializerOptions);
    }

    private static string GetLocaleKey(string file)
    {
        return Path.GetFileNameWithoutExtension(file) ?? throw new InvalidOperationException($"Unable to get locale key from '{file}'.");
    }
}
