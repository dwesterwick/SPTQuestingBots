using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public class NetworkBotDebugInfoSource: IBotDebugInfoSource
    {
        private readonly static ClassVector3 _invalidVector3 = ClassVector3.FromUnityVector3(Vector3.negativeInfinity);

        private static object _staticDataLock = new object();
        private IBotDebugStaticInfo _StaticData;

        private static object _currentDataLock = new object();
        private IBotDebugCurrentInfo? _CurrentData = null;

        private static object _questingDataLock = new object();
        private IBotDebugQuestingInfo? _QuestingData = null;

        public int NetId { get { lock (_staticDataLock) { return _StaticData.NetId; } } }
        public int BotId { get { lock (_staticDataLock) { return _StaticData.BotId; } } }
        public bool IsInitialized { get { lock (_staticDataLock) { return _StaticData.IsInitialized; } } }
        public EBotType BotType { get { lock (_staticDataLock) { return _StaticData.BotType; } } }
        public string BaseBrainName { get { lock (_staticDataLock) { return _StaticData.BaseBrainName; } } }

        public bool HasCurrentData { get { lock (_currentDataLock) { return _CurrentData != null; } } }

        public EBotState BotState { get { lock (_currentDataLock) { return _CurrentData == null ? EBotState.NonActive : _CurrentData.BotState; } } }
        public bool IsSleeping { get { lock (_currentDataLock) { return _CurrentData == null ? false : _CurrentData.IsSleeping; } } }
        public int[] EnemyPlayerIds { get { lock (_currentDataLock) { return _CurrentData == null ? new int[0] : _CurrentData.EnemyPlayerIds; } } }
        public string ActiveLayerName { get { lock (_currentDataLock) { return _CurrentData == null ? string.Empty : _CurrentData.ActiveLayerName; } } }
        public string ActiveLogicName { get { lock (_currentDataLock) { return _CurrentData == null ? string.Empty : _CurrentData.ActiveLogicName; } } }
        public bool HasCurrentEnemy { get { lock (_currentDataLock) { return _CurrentData == null ? false : _CurrentData.HasCurrentEnemy; } } }
        public int CurrentEnemyPlayerId { get { lock (_currentDataLock) { return _CurrentData == null ? -1 : _CurrentData.CurrentEnemyPlayerId; } } }
        public bool HasBoss { get { lock (_currentDataLock) { return _CurrentData == null ? false : _CurrentData.HasBoss; } } }
        public int BossPlayerId { get { lock (_currentDataLock) { return _CurrentData == null ? -1 : _CurrentData.BossPlayerId; } } }
        public bool HasEftPath { get { lock (_currentDataLock) { return _CurrentData == null ? false : _CurrentData.HasEftPath; } } }
        public ClassVector3 CurrentEftPathCorner { get { lock (_currentDataLock) { return _CurrentData == null ? _invalidVector3 : _CurrentData.CurrentEftPathCorner; } } }
        public ClassVector3 CurrentEftPathTarget { get { lock (_currentDataLock) { return _CurrentData == null ? _invalidVector3 : _CurrentData.CurrentEftPathTarget; } } }

        public bool HasQuestingData { get { lock (_questingDataLock) { return _QuestingData != null; } } }

        public bool AllowedToQuest { get { lock (_questingDataLock) { return _QuestingData == null ? false : _QuestingData.AllowedToQuest; } } }
        public bool IsQuesting { get { lock (_questingDataLock) { return _QuestingData == null ? false : _QuestingData.IsQuesting; } } }
        public string QuestName { get { lock (_questingDataLock) { return _QuestingData == null ? string.Empty : _QuestingData.QuestName; } } }
        public string QuestObjectiveName { get { lock (_questingDataLock) { return _QuestingData == null ? string.Empty : _QuestingData.QuestObjectiveName; } } }
        public string QuestObjectiveStepName { get { lock (_questingDataLock) { return _QuestingData == null ? string.Empty : _QuestingData.QuestObjectiveStepName; } } }
        public EJobAssignmentStatus QuestStatus { get { lock (_questingDataLock) { return _QuestingData == null ? EJobAssignmentStatus.NotStarted : _QuestingData.QuestStatus; } } }
        public EBotQuestingDecision CurrentDecision { get { lock (_questingDataLock) { return _QuestingData == null ? EBotQuestingDecision.Inactive : _QuestingData.CurrentDecision; } } }
        public NavMeshPathStatus PathStatus { get { lock (_questingDataLock) { return _QuestingData == null ? NavMeshPathStatus.PathComplete : _QuestingData.PathStatus; } } }

        public NetworkBotDebugInfoSource(IBotDebugStaticInfo staticInfo)
        {
            _StaticData = staticInfo;
        }

        public void UpdateStaticBotInfo(IBotDebugStaticInfo data)
        {
            lock (_staticDataLock)
            {
                _StaticData = data;
            }
        }

        public void UpdateCurrentBotInfo(IBotDebugCurrentInfo data)
        {
            lock (_currentDataLock)
            {
                _CurrentData = data;
            }
        }

        public void UpdateQuestingBotInfo(IBotDebugQuestingInfo data)
        {
            lock (_questingDataLock)
            {
                _QuestingData = data;
            }
        }
    }
}
