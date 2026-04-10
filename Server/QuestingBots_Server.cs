using QuestingBots.Helpers;
using QuestingBots.Utils;
using QuestingBots.Utils.ModIntegrityTests;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace QuestingBots;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET)]
public class QuestingBots_Server : IOnLoad
{
    public const int LOAD_ORDER_OFFSET = 1;

    private LoggingUtil _loggingUtil;
    private ConfigUtil _configUtil;
    private ModIntegrityTestingUtil _modIntegrityTestingUtil;

    public QuestingBots_Server(LoggingUtil loggingUtil, ConfigUtil configUtil, ModIntegrityTestingUtil modIntegrityTestingUtil)
    {
        _loggingUtil = loggingUtil;
        _configUtil = configUtil;
        _modIntegrityTestingUtil = modIntegrityTestingUtil;
    }

    public Task OnLoad()
    {
        RunModIntegrityCheck();

        return Task.CompletedTask;
    }

    private void RunModIntegrityCheck()
    {
        _modIntegrityTestingUtil.AddTest<ClientLibraryExistsTest>(_configUtil);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.Questing.BotQuests.EFTQuests.LevelRange, true);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.BotSpawns.PMCs.FractionOfMaxPlayersVsRaidET, false);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.BotSpawns.PMCs.BotsPerGroupDistribution, true);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.BotSpawns.PMCs.BotDifficultyAsOnline, true);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.BotSpawns.PScavs.BotsPerGroupDistribution, true);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.BotSpawns.PScavs.BotDifficultyAsOnline, true);
        _modIntegrityTestingUtil.AddTest<ArrayIsValidTest>(_configUtil.CurrentConfig.AdjustPScavChance.ChanceVsTimeRemainingFraction, false);

        if (_modIntegrityTestingUtil.RunAllTestsAndVerifyAllPassed())
        {
            return;
        }

        _modIntegrityTestingUtil.LogAllFailureMessages();
        _loggingUtil.Error("Mod integrity check failed. Disabling mod.");

        _configUtil.CurrentConfig.DisableMod();
    }
}
