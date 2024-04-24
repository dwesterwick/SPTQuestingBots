using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Components
{
    public class DebugData : MonoBehaviour
    {
        private Dictionary<JobAssignment, double> jobAssignmentDistances = new Dictionary<JobAssignment, double>();
        private Dictionary<JobAssignment, GameObject> jobAssignmentMarkers = new Dictionary<JobAssignment, GameObject>();
        private Dictionary<JobAssignment, OverlayData> jobAssignmentInfo = new Dictionary<JobAssignment, OverlayData>();
        private Dictionary<BotOwner, OverlayData> botInfo = new Dictionary<BotOwner, OverlayData>();
        private Dictionary<BotOwner, OverlayData> botPathInfo = new Dictionary<BotOwner, OverlayData>();
        private Dictionary<BotOwner, GameObject> botPathMarkers = new Dictionary<BotOwner, GameObject>();

        private readonly float markerRadius = 0.5f;
        private float screenScale = 1.0f;
        private GUIStyle guiStyle;

        public void RegisterBot(BotOwner bot)
        {
            OverlayData botOverlayData = new OverlayData();
            botInfo.Add(bot, botOverlayData);

            OverlayData pathOverlayData = new OverlayData();
            botPathInfo.Add(bot, pathOverlayData);
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

            if (QuestingBotsPluginConfig.ShowBotPathOverlays.Value)
            {
                updateBotPathInfo();
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
            updateBotPathOverlays();
        }

        private void destroyPathMarker(BotOwner bot)
        {
            if (botPathMarkers.ContainsKey(bot))
            {
                Destroy(botPathMarkers[bot]);
                botPathMarkers.Remove(bot);
            }
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

                // Don't update the overlay too often or performance and RAM usage will be affected
                if (botInfo[bot].LastUpdateElapsedTime < 100)
                {
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLabeledValue(Controllers.BotRegistrationManager.GetBotType(bot).ToString(), bot.GetText(), getColorForBotType(bot), Color.white);
                sb.AppendLabeledValue("Layer", bot.Brain.ActiveLayerName(), Color.yellow, Color.yellow);
                sb.AppendLabeledValue("Reason", bot.Brain.GetActiveNodeReason(), Color.white, Color.white);

                BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);

                BotOwner boss = BotHiveMindMonitor.GetBoss(bot);
                if (boss != null)
                {
                    sb.AppendLabeledValue("Boss", boss.GetText(), Color.white, boss.IsDead ? Color.red : Color.white);
                }
                else if (botObjectiveManager?.IsQuestingAllowed == true)
                {
                    BotJobAssignment botJobAssignment = BotJobAssignmentFactory.GetCurrentJobAssignment(bot, false);

                    sb.AppendLabeledValue("Quest", botJobAssignment.QuestAssignment?.ToString(), Color.cyan, Color.cyan);
                    sb.AppendLabeledValue("Objective", botJobAssignment.QuestObjectiveAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Step", botJobAssignment.QuestObjectiveStepAssignment?.ToString(), Color.white, Color.white);
                    sb.AppendLabeledValue("Status", botJobAssignment.Status.ToString(), Color.white, Color.white);
                }

                if (botObjectiveManager != null)
                {
                    if (botObjectiveManager.NotQuestingReason != NotQuestingReason.None)
                    {
                        sb.AppendLabeledValue("NotQuestingReason", botObjectiveManager.NotQuestingReason.ToString(), Color.white, Color.white);
                    }
                    if (botObjectiveManager.NotFollowingReason != NotQuestingReason.None)
                    {
                        sb.AppendLabeledValue("NotFollowingReason", botObjectiveManager.NotFollowingReason.ToString(), Color.white, Color.white);
                    }
                }

                botInfo[bot].StaticText = sb.ToString();

                botInfo[bot].ResetUpdateTime();
            }
        }

        private void updateBotPathInfo()
        {
            foreach (BotOwner bot in botPathInfo.Keys.ToArray())
            {
                if ((bot == null) || bot.IsDead)
                {
                    botPathInfo.Remove(bot);
                    destroyPathMarker(bot);

                    continue;
                }

                // Don't update the overlay too often or performance and RAM usage will be affected
                if (botPathInfo[bot].LastUpdateElapsedTime < 100)
                {
                    continue;
                }

                // Check if a path has been defined for the bot by this mod
                BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);
                if ((botObjectiveManager?.BotPath == null) || !botObjectiveManager.BotPath.HasPath)
                {
                    if (botPathMarkers.ContainsKey(bot))
                    {
                        botPathMarkers[bot].SetActive(false);
                    }
                    continue;
                }

                // Ensure the bot has an active quest and is not a follower
                if (!botObjectiveManager.IsQuestingAllowed || !botObjectiveManager.IsJobAssignmentActive || BotHiveMindMonitor.HasBoss(bot))
                {
                    if (botPathMarkers.ContainsKey(bot))
                    {
                        botPathMarkers[bot].SetActive(false);
                    }
                    continue;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLabeledValue("Target Position", botObjectiveManager.BotPath.TargetPosition.ToString(), Color.white, Color.white);
                sb.AppendLabeledValue("Bot", bot.GetText(), Color.white, Color.white);
                sb.AppendLabeledValue("Status", botObjectiveManager.BotPath.Status.ToString(), Color.white, getColorForPathStatus(botObjectiveManager.BotPath.Status));

                botPathInfo[bot].StaticText = sb.ToString();
                botPathInfo[bot].Position = botObjectiveManager.BotPath.TargetPosition;
                botPathInfo[bot].ResetUpdateTime();
                
                if (!botPathMarkers.ContainsKey(bot))
                {
                    botPathMarkers.Add(bot, DebugHelpers.CreateSphere(botPathInfo[bot].Position, markerRadius * 2, Color.green));
                }
                else
                {
                    botPathMarkers[bot].transform.position = botPathInfo[bot].Position;
                }
                botPathMarkers[bot].SetActive(true);
            }
        }

        private static Color getColorForBotType(BotOwner bot)
        {
            if ((bot == null) || bot.IsDead)
            {
                return Color.white;
            }

            Color botTypeColor = Color.green;

            // If you're dead, there's no reason to worry about overlay colors
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return botTypeColor;
            }

            // Check if the bot doesn't like you
            if (bot.EnemiesController?.EnemyInfos?.Any(i => i.Value.ProfileId == mainPlayer.ProfileId) == true)
            {
                botTypeColor = Color.red;
            }

            return botTypeColor;
        }

        private static Color getColorForPathStatus(NavMeshPathStatus status)
        {
            switch (status)
            {
                case NavMeshPathStatus.PathComplete: return Color.green;
                case NavMeshPathStatus.PathPartial: return Color.yellow;
                default: return Color.red;
            }
        }

        private void updateBotOverlays()
        {
            if (!QuestingBotsPluginConfig.ShowBotInfoOverlays.Value)
            {
                return;
            }

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (mainPlayer == null)
            {
                return;
            }

            foreach (BotOwner bot in botInfo.Keys.ToArray())
            {
                if ((bot == null) || bot.IsDead)
                {
                    continue;
                }

                Vector3 botHeadPosition = bot.Position + new Vector3(0, 1.5f, 0);
                Vector3 screenPos = Camera.main.WorldToScreenPoint(botHeadPosition);
                if (screenPos.z <= 0)
                {
                    continue;
                }

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

        private void updateStaticJobAssignmentDistances()
        {
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            
            foreach (JobAssignment jobAssignment in jobAssignmentDistances.Keys.ToArray())
            {
                if (mainPlayer == null)
                {
                    jobAssignmentDistances[jobAssignment] = double.PositiveInfinity;
                }
                else
                {
                    jobAssignmentDistances[jobAssignment] = Math.Round(Vector3.Distance(mainPlayer.Position, jobAssignment.Position.Value), 1);
                }
            }
        }

        private void updateStaticJobAssignmentOverlays()
        {
            foreach (JobAssignment jobAssignment in jobAssignmentMarkers.Keys.ToArray())
            {
                // Set by updateStaticJobAssignmentMarkerVisibility()
                if (!jobAssignmentMarkers[jobAssignment].activeSelf)
                {
                    continue; 
                }

                Vector3 screenPos = Camera.main.WorldToScreenPoint(jobAssignmentInfo[jobAssignment].Position);
                if (screenPos.z <= 0)
                {
                    continue;
                }

                // Set by updateStaticJobAssignmentDistances()
                jobAssignmentInfo[jobAssignment].GuiContent.text = jobAssignmentInfo[jobAssignment].StaticText + jobAssignmentDistances[jobAssignment];

                Vector2 guiSize = guiStyle.CalcSize(jobAssignmentInfo[jobAssignment].GuiContent);
                float x = (screenPos.x * screenScale) - (guiSize.x / 2);
                float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                Rect rect = new Rect(new Vector2(x, y), guiSize);
                jobAssignmentInfo[jobAssignment].GuiRect = rect;

                GUI.Box(jobAssignmentInfo[jobAssignment].GuiRect, jobAssignmentInfo[jobAssignment].GuiContent, guiStyle);
            }
        }

        private void updateBotPathOverlays()
        {
            foreach (BotOwner bot in botPathMarkers.Keys.ToArray())
            {
                if (!QuestingBotsPluginConfig.ShowBotPathOverlays.Value)
                {
                    destroyPathMarker(bot);
                    continue;
                }

                if ((bot == null) || bot.IsDead)
                {
                    continue;
                }

                // Set by updateBotPathInfo()
                if (!botPathMarkers[bot].activeSelf)
                {
                    continue;
                }

                Vector3 screenPos = Camera.main.WorldToScreenPoint(botPathInfo[bot].Position);
                if (screenPos.z <= 0)
                {
                    continue;
                }

                // Copy the text here in case we want to add dynamic text in the future
                botPathInfo[bot].GuiContent.text = botPathInfo[bot].StaticText;

                Vector2 guiSize = guiStyle.CalcSize(botPathInfo[bot].GuiContent);
                float x = (screenPos.x * screenScale) - (guiSize.x / 2);
                float y = Screen.height - ((screenPos.y * screenScale) + guiSize.y);
                Rect rect = new Rect(new Vector2(x, y), guiSize);
                botPathInfo[bot].GuiRect = rect;

                GUI.Box(botPathInfo[bot].GuiRect, botPathInfo[bot].GuiContent, guiStyle);
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
            Quest lastQuest = null;
            foreach (JobAssignment jobAssignment in jobAssignments)
            {
                // Ensure the position is valid and isn't the same as the previous step in the quest objective
                Vector3? stepPosition = jobAssignment.Position;
                if (!stepPosition.HasValue || (stepPosition == lastPosition))
                {
                    continue;
                }

                string questText = "Quest: " + jobAssignment.QuestAssignment.ToString();
                questText += "\nObjective: " + jobAssignment.QuestObjectiveAssignment.ToString();
                questText += "\nStep: " + jobAssignment.QuestObjectiveStepAssignment.ToString();
                questText += "\nDistance: ";

                addJobAssignment(jobAssignment, questText, stepPosition.Value, Color.red);

                if (lastQuest != jobAssignment.QuestAssignment)
                {
                    IList<Vector3> waypoints = jobAssignment.QuestAssignment.GetWaypointPositions();
                    for (int w = 0; w < waypoints.Count; w++)
                    {
                        questText = "Quest: " + jobAssignment.QuestAssignment.ToString();
                        questText += "\nWaypoint #" + (w + 1) + ": " + waypoints[w];
                        questText += "\nDistance: ";

                        JobAssignment clonedAssignment = (JobAssignment)jobAssignment.Clone();
                        addJobAssignment(clonedAssignment, questText, waypoints[w], Color.blue);
                    }
                }

                lastPosition = stepPosition.Value;
                lastQuest = jobAssignment.QuestAssignment;
            }

            LoggingController.LogInfo("Loading all possible job assignments...done (Created " + jobAssignmentDistances.Count + " markers).");
        }

        private void addJobAssignment(JobAssignment jobAssignment, string questText, Vector3 position, Color markerColor)
        {
            Vector3 overlayPosition = position + new Vector3(0, markerRadius + 0.1f, 0);
            OverlayData overlayData = new OverlayData(overlayPosition, questText);

            jobAssignmentDistances.Add(jobAssignment, float.PositiveInfinity);
            jobAssignmentMarkers.Add(jobAssignment, DebugHelpers.CreateSphere(position, markerRadius * 2, markerColor));
            jobAssignmentInfo.Add(jobAssignment, overlayData);
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
