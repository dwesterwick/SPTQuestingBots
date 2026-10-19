using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public class NetworkBotDebugInfoSource: IBotDebugInfoSource
    {
        private int _netId;
        public int NetId => _netId;

        private int _botId;
        public int BotId => _botId;

        private string _botName = null!;
        public string BotName => _botName;

        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;

        private EBotType _botType;
        public EBotType BotType => _botType;

        private string _baseBrainName = null!;
        public string BaseBrainName => _baseBrainName;

        public bool HasCurrentData { get; private set; } = false;

        private EBotState _botState = EBotState.NonActive;
        public EBotState BotState => _botState;

        private bool _isSleeping = false;
        public bool IsSleeping => _isSleeping;

        private int[] _enemyPlayerIds = Array.Empty<int>();
        public int[] EnemyPlayerIds => _enemyPlayerIds;

        private string _activeLayerName = string.Empty;
        public string ActiveLayerName => _activeLayerName;

        private string _activeLogicName = string.Empty;
        public string ActiveLogicName => _activeLogicName;

        private bool _hasCurrentEnemy = false;
        public bool HasCurrentEnemy => _hasCurrentEnemy;

        private int _currentEnemyPlayerId = -1;
        public int CurrentEnemyPlayerId => _currentEnemyPlayerId;

        private bool _hasBoss = false;
        public bool HasBoss => _hasBoss;

        private int _bossPlayerId = -1;
        public int BossPlayerId => _bossPlayerId;

        private bool _hasEftPath = false;
        public bool HasEftPath => _hasEftPath;

        private ClassVector3 _currentEftPathCorner = ClassVector3.FromUnityVector3(Vector3.negativeInfinity);
        public ClassVector3 CurrentEftPathCorner => _currentEftPathCorner;

        private ClassVector3 _currentEftPathTarget = ClassVector3.FromUnityVector3(Vector3.negativeInfinity);
        public ClassVector3 CurrentEftPathTarget => _currentEftPathTarget;

        public bool HasQuestingData { get; private set; } = false;

        private bool _allowedToQuest = false;
        public bool AllowedToQuest => _allowedToQuest;

        private bool _isQuesting = false;
        public bool IsQuesting => _isQuesting;

        private string _questName = string.Empty;
        public string QuestName => _questName;

        private string _questObjectiveName = string.Empty;
        public string QuestObjectiveName => _questObjectiveName;

        private string _questObjectiveStepName = string.Empty;
        public string QuestObjectiveStepName => _questObjectiveStepName;

        private EJobAssignmentStatus _questStatus = EJobAssignmentStatus.NotStarted;
        public EJobAssignmentStatus QuestStatus => _questStatus;

        private EBotQuestingDecision _currentDecision = EBotQuestingDecision.Inactive;
        public EBotQuestingDecision CurrentDecision => _currentDecision;

        private UnityEngine.AI.NavMeshPathStatus _pathStatus = UnityEngine.AI.NavMeshPathStatus.PathComplete;
        public UnityEngine.AI.NavMeshPathStatus PathStatus => _pathStatus;

        private bool _hasQuestingPath = false;
        public bool HasQuestingPath => _hasQuestingPath;

        private ClassVector3 _currentQuestingPathTarget = ClassVector3.FromUnityVector3(Vector3.negativeInfinity);
        public ClassVector3 CurrentQuestingPathTarget => _currentQuestingPathTarget;

        private Vector3[] _currentQuestingPath = Array.Empty<Vector3>();
        public Vector3[] CurrentQuestingPath => _currentQuestingPath;

        public NetworkBotDebugInfoSource(IBotDebugStaticInfo staticInfo)
        {
            UpdateStaticBotInfo(staticInfo);
        }

        public void UpdateStaticBotInfo(IBotDebugStaticInfo data)
        {
            _netId = data.NetId;
            _botId = data.BotId;
            if (_botName != data.BotName) { _botName = data.BotName; }
            _isInitialized = data.IsInitialized;
            _botType = data.BotType;
            if (_baseBrainName != data.BaseBrainName) { _baseBrainName = data.BaseBrainName; }
        }

        public void UpdateCurrentBotInfo(IBotDebugCurrentInfo data)
        {
            _botState = data.BotState;
            _isSleeping = data.IsSleeping;
            if (_enemyPlayerIds != data.EnemyPlayerIds) { _enemyPlayerIds = data.EnemyPlayerIds; }
            if (_activeLayerName != data.ActiveLayerName) { _activeLayerName = data.ActiveLayerName; }
            if (_activeLogicName != data.ActiveLogicName) { _activeLogicName = data.ActiveLogicName; }
            _hasCurrentEnemy = data.HasCurrentEnemy;
            _currentEnemyPlayerId = data.CurrentEnemyPlayerId;
            _hasBoss = data.HasBoss;
            _bossPlayerId = data.BossPlayerId;
            _hasEftPath = data.HasEftPath;
            if (!_currentEftPathCorner.IsSameAs(data.CurrentEftPathCorner)) { _currentEftPathCorner = data.CurrentEftPathCorner; }
            if (!_currentEftPathTarget.IsSameAs(data.CurrentEftPathTarget)) { _currentEftPathTarget = data.CurrentEftPathTarget; }

            HasCurrentData = true;
        }

        public void UpdateQuestingBotInfo(IBotDebugQuestingInfo data)
        {
            _allowedToQuest = data.AllowedToQuest;
            _isQuesting = data.IsQuesting;
            if (_questName != data.QuestName) { _questName = data.QuestName; }
            if (_questObjectiveName != data.QuestObjectiveName) { _questObjectiveName = data.QuestObjectiveName; }
            if (_questObjectiveStepName != data.QuestObjectiveStepName) { _questObjectiveStepName = data.QuestObjectiveStepName; }
            _questStatus = data.QuestStatus;
            _currentDecision = data.CurrentDecision;
            _pathStatus = data.PathStatus;
            _hasQuestingPath = data.HasQuestingPath;
            if (!_currentQuestingPathTarget.IsSameAs(data.CurrentQuestingPathTarget)) { _currentQuestingPathTarget = data.CurrentQuestingPathTarget; }
            if (_currentQuestingPath != data.CurrentQuestingPath) { _currentQuestingPath = data.CurrentQuestingPath; }

            HasQuestingData = true;
        }
    }
}
