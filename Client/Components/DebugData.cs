using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using QuestingBots.Utils.Benchmarking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Components
{
    public class DebugData : MonoBehaviour
    {
        public static List<string> BotFilter = new List<string>();
        private readonly static float markerRadius = 0.5f;

        private List<AbstractDebugGizmo> _gizmos = new List<AbstractDebugGizmo>();
        private Dictionary<int, IBotDebugInfoSource> _botDebugInfoSources = new Dictionary<int, IBotDebugInfoSource>();

        private int jobAssignmentGizmoCount => _gizmos.Count(gizmo => gizmo is Models.DebugGizmos.JobAssignmentGizmo);

        [Benchmark]
        public void RegisterLocalBot(BotOwner bot)
        {
            LocalBotDebugInfoSource source = new LocalBotDebugInfoSource(bot);
            RegisterBot_Internal(source);

            if (!TryRegisterBotDebugInfo(source))
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not register bot debug info for " + bot.GetText());
            }

            //_gizmos.Add(new Models.DebugGizmos.BotInfoGizmo(bot));
            _gizmos.Add(new Models.DebugGizmos.BotPathVisualizationGizmo(bot, Color.magenta));

            BotObjectiveManager? botObjectiveManager = bot.GetObjectiveManager();
            if (botObjectiveManager != null)
            {
                _gizmos.Add(new Models.DebugGizmos.BotObjectivePositionMarkerGizmo(bot, markerRadius));
                _gizmos.Add(new Models.DebugGizmos.BotPathTargetMarkerGizmo(bot, markerRadius));
                _gizmos.Add(new Models.DebugGizmos.BotPathCurrentCornerMarkerGizmo(bot, markerRadius));
            }
        }

        private void RegisterBot_Internal(IBotDebugInfoSource source)
        {
            _botDebugInfoSources.Add(source.NetId, source);
            _gizmos.Add(new NetworkBotDebugInfoGizmo(source));
        }

        private bool TryRegisterBotDebugInfo(IBotDebugStaticInfo data)
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return true;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(data.NetId);
            if (botPlayer == null)
            {
                return false;
            }

            Singleton<PlayerData>.Instance.RegisterBotDebugInfo(botPlayer, data);
            return true;
        }

        public bool TryUpdateClientBotStaticInfo(IBotDebugStaticInfo data)
        {
            if (_botDebugInfoSources.TryGetValue(data.NetId, out IBotDebugInfoSource source))
            {
                NetworkBotDebugInfoSource? networkBotDebugInfoSource = source as NetworkBotDebugInfoSource;
                if (networkBotDebugInfoSource == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot update IBotDebugInfoSource using network packet for bot with NetID=" + data.NetId);
                    return false;
                }

                networkBotDebugInfoSource.UpdateStaticBotInfo(data);
                return true;
            }

            if (!TryRegisterBotDebugInfo(data))
            {
                return false;
            }

            source = new NetworkBotDebugInfoSource(data);
            RegisterBot_Internal(source);

            return true;
        }

        public void UpdateClientBotCurrentInfo(IBotDebugCurrentInfo data)
        {
            if (_botDebugInfoSources.TryGetValue(data.NetId, out IBotDebugInfoSource source))
            {
                NetworkBotDebugInfoSource? networkBotDebugInfoSource = source as NetworkBotDebugInfoSource;
                if (networkBotDebugInfoSource == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot update IBotDebugInfoSource using network packet for bot with NetID=" + data.NetId);
                    return;
                }

                networkBotDebugInfoSource.UpdateCurrentBotInfo(data);
            }
        }

        public void UpdateClientBotQuestingInfo(IBotDebugQuestingInfo data)
        {
            if (_botDebugInfoSources.TryGetValue(data.NetId, out IBotDebugInfoSource source))
            {
                NetworkBotDebugInfoSource? networkBotDebugInfoSource = source as NetworkBotDebugInfoSource;
                if (networkBotDebugInfoSource == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot update IBotDebugInfoSource using network packet for bot with NetID=" + data.NetId);
                    return;
                }

                networkBotDebugInfoSource.UpdateQuestingBotInfo(data);
            }
        }

        protected void Awake()
        {
            QuestingBotsPluginConfig.QuestOverlayFontSize.SettingChanged += (object sender, EventArgs e) => { updateGuiStyles(sender, e); };
            QuestingBotsPluginConfig.BotFilter.SettingChanged += (object sender, EventArgs e) =>
            {
                ValidateBotFilter(QuestingBotsPluginConfig.BotFilter.Value);
            };

            // Reset to default since filter from previous raid won't matter in the next
            QuestingBotsPluginConfig.BotFilter.BoxedValue = "";

            _gizmos.Add(new PlayerCoordinatesGizmo());
            _gizmos.Add(new GameTimeGizmo());
        }

        private void updateGuiStyles(object sender, EventArgs e) => _gizmos.ForEach(gizmo => gizmo.UpdateGUIStyle());

        protected void Update()
        {
            if (RaidHelpers.IsHostRaid())
            {
                if (!Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>().HaveQuestsBeenBuilt)
                {
                    return;
                }

                if (QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value && (jobAssignmentGizmoCount == 0))
                {
                    loadAllPossibleJobAssignments();
                }
            }

            removeUnneededGizmos();

            foreach (AbstractDebugGizmo gizmo in _gizmos)
            {
                gizmo.Update();
            }
        }

        protected void OnGUI()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            foreach (AbstractDebugGizmo gizmo in _gizmos)
            {
                gizmo.Draw();
            }
        }

        private void removeUnneededGizmos()
        {
            foreach (AbstractDebugGizmo gizmo in _gizmos.Where(gizmo => gizmo.ReadyToDispose()))
            {
                NetworkBotDebugInfoGizmo? networkBotDebugInfoGizmo = gizmo as NetworkBotDebugInfoGizmo;
                if (networkBotDebugInfoGizmo != null)
                {
                    _botDebugInfoSources.Remove(networkBotDebugInfoGizmo.BotNetId);
                }

                gizmo.Dispose();
            }

            _gizmos.RemoveAll(gizmo => gizmo.ReadyToDispose());
        }

        private void loadAllPossibleJobAssignments()
        {
            Singleton<LoggingUtil>.Instance.LogInfo("Loading all possible job assignments...");

            IEnumerable<JobAssignment> jobAssignments = BotJobAssignmentController.CreateAllPossibleJobAssignments();

            Vector3 lastPosition = Vector3.positiveInfinity;
            BotQuest lastQuest = null!;
            foreach (JobAssignment jobAssignment in jobAssignments)
            {
                // Ensure the position is valid and isn't the same as the previous step in the quest objective
                Vector3? stepPosition = jobAssignment.Position;
                if (!stepPosition.HasValue || (stepPosition == lastPosition))
                {
                    continue;
                }

                addGizmosForQuestStep(jobAssignment, stepPosition.Value);

                if (lastQuest != jobAssignment.QuestAssignment)
                {
                    IList<Vector3> waypoints = jobAssignment.QuestAssignment.GetWaypointPositions();
                    for (int w = 0; w < waypoints.Count; w++)
                    {
                        addGizmosForQuestWaypoint(jobAssignment, waypoints[w], w + 1);
                    }
                }

                lastPosition = stepPosition.Value;
                lastQuest = jobAssignment.QuestAssignment;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading all possible job assignments...done (Created " + jobAssignmentGizmoCount + " markers).");
        }

        private void addGizmosForQuestStep(JobAssignment jobAssignment, Vector3 position)
        {
            string questText = "Quest: " + jobAssignment.QuestAssignment.ToString();
            questText += "\nObjective: " + jobAssignment.QuestObjectiveAssignment.ToString();
            questText += "\nStep: " + jobAssignment.QuestObjectiveStepAssignment.ToString();
            questText += "\nDistance: ";

            _gizmos.Add(new JobAssignmentGizmo(jobAssignment, position, questText, markerRadius, Color.red));
        }

        private void addGizmosForQuestWaypoint(JobAssignment jobAssignment, Vector3 position, int waypointNumber)
        {
            string questText = "Quest: " + jobAssignment.QuestAssignment.ToString();
            questText += "\nWaypoint #" + waypointNumber + ": " + position;
            questText += "\nDistance: ";

            _gizmos.Add(new JobAssignmentGizmo(jobAssignment, position, questText, markerRadius, Color.blue));
        }

        private static void ValidateBotFilter(string configString)
        {
            BotFilter.Clear();
            if (configString.IsNullOrEmpty())
                return;

            string[] bounds = configString.Split(',');
            foreach (var bound in bounds)
            {
                if (int.TryParse(bound, out int _))
                {
                    string botName = "Bot" + bound.Trim();
                    BotFilter.Add(botName);
                    continue;
                }
                //Singleton<LoggingUtil>.Instance.LogError("DebugData::ValidateBotFilter Unable to parse bot filters.");
                BotFilter.Clear();
            }
        }
    }
}
