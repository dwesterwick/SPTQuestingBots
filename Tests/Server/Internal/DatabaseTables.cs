using SPTarkov.Server.Core.Models.Spt.Tables;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Server.Internal;

// Copied from https://github.com/sp-tarkov/server-csharp/blob/main/SPTarkov.Server/Helpers/DatabaseTables.cs

public sealed record DatabaseTables
{
    public required BotTable Bots { get; init; }

    public required HideoutTable Hideout { get; init; }

    public required LocaleTable Locales { get; init; }

    public required LocationTable Locations { get; init; }

    public required MatchTable Match { get; init; }

    public required TemplateTable Templates { get; init; }

    public required TradersTable Traders { get; init; }

    public required GlobalTable Globals { get; init; }

    public required ServerTable Server { get; init; }

    public required SettingsTable Settings { get; init; }
}
