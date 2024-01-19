using ChatShared;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Components
{
    public class DebugData : MonoBehaviour
    {
        private Dictionary<JobAssignment, double> jobAssignmentDistances = new Dictionary<JobAssignment, double>();
        private Dictionary<JobAssignment, GameObject> jobAssignmentMarkers = new Dictionary<JobAssignment, GameObject>();
        private Dictionary<JobAssignment, OverlayData> jobAssignmentInfo = new Dictionary<JobAssignment, OverlayData>();
        private Dictionary<BotOwner, OverlayData> botInfo = new Dictionary<BotOwner, OverlayData>();

        private readonly float markerRadius = 0.5f;
        private float screenScale = 1.0f;
        private GUIStyle guiStyle;

        public void RegisterBot(BotOwner bot)
        {
            OverlayData overlayData = new OverlayData();
            botInfo.Add(bot, overlayData);
        }

        private void Awake()
        {
            QuestingBotsPluginConfig.QuestOverlayFontSize.SettingChanged += (object sender, EventArgs e) => { guiStyle = DebugHelpers.CreateGuiStyle(); };
        }

        private void Update()
        {
            if (!Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                return;
            }

            if (QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value && (jobAssignmentDistances.Count == 0))
            {
                loadAllPossibleJobAssignments();
            }

            if (QuestingBotsPluginConfig.ShowBotInfoOverlays.Value)
            {
                updateBotInfo();
            }
        }

        private void OnGUI()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            if (guiStyle == null)
            {
                guiStyle = DebugHelpers.CreateGuiStyle();
            }

            updateStaticJobAssignmentDistances();
            updateStaticJobAssignmentMarkerVisibility();
            updateStaticJobAssignmentOverlays();

            updateBotOverlays();
        }

        private void updateBotInfo()
        {
            foreach (BotOwner bot in botInfo.Keys.ToArray())
            {
                if ((bot == null) || bot.IsDead)
                {
                    botInfo.Remove(bot);
                    continue;
                }

                if (botInfo[bot].LastUpdateElapsedTime < 100)
                {
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLabeledValue(Controllers.BotRegistrationManager.GetBotType(bot).ToString(), bot.GetText(), getColorForBotType(bot), Color.white);
                sb.AppendLabeledValue("Layer", bot.Brain.ActiveLayerName(), Color.yellow, Color.yellow);
                sb.AppendLabeledValue("Reason", bot.Brain.GetActiveNodeReason(), Color.white, Color.white);

                BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);
                if (botObjectiveManager?.IsQuestingAllowed == true)
                {
                    BotJobAssignment botJobAssignment = BotJobAssignmentFactory.GetCurrentJobAssignment(bot);

                    sb.AppendLabeledValue("Quest", botJobAssignment.QuestAssignment?.ToString(), Color.cyan, Color.cyan);
                    sb.AppendLabeledValue("Objective", botJobAssignment.QuestObjectiveAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Step", botJobAssignment.QuestObjectiveStepAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Status", botJobAssignment.Status.ToString(), Color.white, Color.white);
                }

                botInfo[bot].StaticText = sb.ToString();

                botInfo[bot].ResetUpdateTime();
            }
        }

        private static Color getColorForBotType(BotOwner bot)
        {
            if (bot == null)
            {
                return Color.white;
            }

            Color botTypeColor = Color.green;

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return botTypeColor;
            }

            if (bot.EnemiesController?.EnemyInfos?.Any(i => i.Value.ProfileId == mainPlayer.ProfileId) == true)
            {
                botTypeColor = Color.red;
            }

            return botTypeColor;
        }

        private void updateBotOverlays()
        {
            if (!QuestingBotsPluginConfig.ShowBotInfoOverlays.Value)
            {
                return;
            }

            foreach (BotOwner bot in botInfo.Keys.ToArray())
            {
                Vector3 botHeadPosition = bot.Position + new Vector3(0, 1.5f, 0);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(botHeadPosition);
                if (screenPos.z <= 0)
                {
                    continue;
                }

                Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                double distanceToBot = Math.Round(Vector3.Distance(bot.Position, mainPlayer.Position), 1);

                StringBuilder sb = new StringBuilder();
                sb.Append(botInfo[bot].StaticText);
                sb.AppendLabeledValue("Distance", distanceToBot + "m", Color.white, Color.white);
                botInfo[bot].GuiContent.text = sb.ToString();

                Vector2 guiSize = guiStyle.CalcSize(botInfo[bot].GuiContent);
                float x = (screenPos.x * screenScale) - (guiSize.x / 2);
                float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                Rect rect = new Rect(new Vector2(x, y), guiSize);
                botInfo[bot].GuiRect = rect;

                GUI.Box(botInfo[bot].GuiRect, botInfo[bot].GuiContent, guiStyle);
            }
        }

        private void updateStaticJobAssignmentMarkerVisibility()
        {
            foreach (JobAssignment jobAssignment in jobAssignmentMarkers.Keys.ToArray())
            {
                bool isVisible = QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value;
                isVisible &= jobAssignmentDistances[jobAssignment] <= QuestingBotsPluginConfig.QuestOverlayMaxDistance.Value;
                isVisible &= QuestingBotsPluginConfig.ShowQuestInfoForSpawnSearchQuests.Value || !jobAssignment.IsSpawnSearchQuest;

                jobAssignmentMarkers[jobAssignment].SetActive(isVisible);
            }
        }

        private void updateStaticJobAssignmentOverlays()
        {
            foreach (JobAssignment jobAssignment in jobAssignmentMarkers.Keys.ToArray())
            {
                if (!jobAssignmentMarkers[jobAssignment].activeSelf)
                {
                    continue; 
                }

                Vector3 screenPos = Camera.main.WorldToScreenPoint(jobAssignmentInfo[jobAssignment].Position);
                if (screenPos.z <= 0)
                {
                    continue;
                }

                jobAssignmentInfo[jobAssignment].GuiContent.text = jobAssignmentInfo[jobAssignment].StaticText + jobAssignmentDistances[jobAssignment];

                Vector2 guiSize = guiStyle.CalcSize(jobAssignmentInfo[jobAssignment].GuiContent);
                float x = (screenPos.x * screenScale) - (guiSize.x / 2);
                float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                Rect rect = new Rect(new Vector2(x, y), guiSize);
                jobAssignmentInfo[jobAssignment].GuiRect = rect;

                GUI.Box(jobAssignmentInfo[jobAssignment].GuiRect, jobAssignmentInfo[jobAssignment].GuiContent, guiStyle);
            }
        }

        private void updateStaticJobAssignmentDistances()
        {
            Vector3 mainPlayerPosition = Singleton<GameWorld>.Instance.MainPlayer.Position;
            foreach (JobAssignment jobAssignment in jobAssignmentDistances.Keys.ToArray())
            {
                jobAssignmentDistances[jobAssignment] = Math.Round(Vector3.Distance(mainPlayerPosition, jobAssignment.Position.Value), 1);
            }
        }

        private void loadAllPossibleJobAssignments()
        {
            // If DLSS or FSR are enabled, set a screen scale value
            if (CameraClass.Instance.SSAA.isActiveAndEnabled)
            {
                screenScale = (float)CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
            }

            LoggingController.LogInfo("Loading all possible job assignments...");

            IEnumerable<JobAssignment> jobAssignments = BotJobAssignmentFactory.CreateAllPossibleJobAssignments();

            Vector3 lastPosition = Vector3.positiveInfinity;
            foreach (JobAssignment jobAssignment in jobAssignments)
            {
                Vector3? stepPosition = jobAssignment.Position;
                if (!stepPosition.HasValue || (stepPosition == lastPosition))
                {
                    continue;
                }

                string questText = "Quest: " + jobAssignment.QuestAssignment.ToString();
                questText += "\nObjective: " + jobAssignment.QuestObjectiveAssignment.ToString();
                questText += "\nStep: " + jobAssignment.QuestObjectiveStepAssignment.ToString();
                questText += "\nDistance: ";

                Vector3 overlayPosition = stepPosition.Value + new Vector3(0, markerRadius + 0.1f, 0);
                OverlayData overlayData = new OverlayData(overlayPosition, questText);

                jobAssignmentDistances.Add(jobAssignment, float.PositiveInfinity);
                jobAssignmentMarkers.Add(jobAssignment, DebugHelpers.CreateSphere(stepPosition.Value, markerRadius * 2, Color.red));
                jobAssignmentInfo.Add(jobAssignment, overlayData);

                lastPosition = stepPosition.Value;
            }

            LoggingController.LogInfo("Loading all possible job assignments...done (Created " + jobAssignmentDistances.Count + " markers).");
        }

        internal class OverlayData
        {
            public ActorDataStruct Data { get; set; }
            public GUIContent GuiContent { get; set; }
            public Rect GuiRect { get; set; }
            public Vector3 Position { get; set; }
            public string StaticText { get; set; } = "";

            private Stopwatch updateTimer = Stopwatch.StartNew();

            public long LastUpdateElapsedTime => updateTimer.ElapsedMilliseconds;

            public OverlayData()
            {
                GuiContent = new GUIContent();
                GuiRect = new Rect();
            }

            public OverlayData(Vector3 _position) : this()
            {
                Position = _position;
            }

            public OverlayData(Vector3 _position, string _staticText) : this(_position)
            {
                StaticText = _staticText;
            }

            public void ResetUpdateTime()
            {
                updateTimer.Restart();
            }
        }
    }
}
