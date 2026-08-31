using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ProfileUtil
    {
        private GlobalTable _globalTable;
        private ProfileHelper _profileHelper;
        private TimeUtil _timeUtil;
        private FenceService _fenceService;

        public ProfileUtil(GlobalTable globalTable, ProfileHelper profileHelper, TimeUtil timeUtil, FenceService fenceService)
        {
            _globalTable = globalTable;
            _profileHelper = profileHelper;
            _timeUtil = timeUtil;
            _fenceService = fenceService;
        }

        public PmcData? GetPmcProfile(MongoId sessionId) => _profileHelper.GetPmcProfile(sessionId);
        public PmcData? GetScavProfile(MongoId sessionId) => _profileHelper.GetScavProfile(sessionId);

        public int GetBaseScavCooldownTime() => _globalTable.Configuration.SavagePlayCooldown;
        public double GetMaxScavCooldownTime(PmcData pmcData) => GetBaseScavCooldownTime() * GetTotalScavCooldownTimeModifier(pmcData);

        public double GetTotalScavCooldownTimeModifier(PmcData pmcData)
        {
            if (pmcData?.Bonuses == null)
            {
                throw new InvalidOperationException("Cannot retrieve PMC bonus information");
            }

            double pmcBonusModifierTotal = pmcData.Bonuses
                .Where(x => x.Type == BonusType.ScavCooldownTimer)
                .Where(x => x.Value != null)
                .Sum(bonus => bonus.Value!.Value / 100);

            FenceLevel? fenceInfo = _fenceService.GetFenceInfo(pmcData);
            if (fenceInfo == null)
            {
                throw new InvalidOperationException("Cannot retrieve Fence information for PMC profile");
            }

            return (1 + pmcBonusModifierTotal) * fenceInfo.SavageCooldownModifier;
        }

        public double? GetScavCooldownTimeRemaining(PmcData scavData)
        {
            if (scavData?.Info?.SavageLockTime == null)
            {
                throw new InvalidOperationException("SavageLockTime is null");
            }

            long now = _timeUtil.GetTimeStamp();
            double timeUtilScavIsAvailable = Math.Max(0, (double)(scavData!.Info!.SavageLockTime! - now));

            return timeUtilScavIsAvailable;
        }
    }
}
