using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers.Bots;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Controllers
{
    public class DebugController : MonoBehaviour
    {
        private static Dictionary<JobAssignment, double> jobAssignmentDistances = new Dictionary<JobAssignment, double>();
        private static Dictionary<JobAssignment, GameObject> jobAssignmentMarkers = new Dictionary<JobAssignment, GameObject>();
        private static Dictionary<JobAssignment, QuestOverlayData> jobAssignmentInfo = new Dictionary<JobAssignment, QuestOverlayData>();

        private readonly float markerRadius = 0.5f;
        private float screenScale = 1.0f;
        private GUIStyle guiStyle;

        private void clear()
        {
            jobAssignmentMarkers.ExecuteForEach(m => Destroy(m.Value));
            jobAssignmentMarkers.Clear();
            jobAssignmentDistances.Clear();
        }

        private void Awake()
        {
            QuestingBotsPluginConfig.QuestOverlayFontSize.SettingChanged += (object sender, EventArgs e) => { guiStyle = DebugHelpers.CreateGuiStyle(); };
        }

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                clear();
                return;
            }

            if (!QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value)
            {
                return;
            }

            if (!BotQuestBuilder.HaveQuestsBeenBuilt)
            {
                return;
            }

            if (jobAssignmentDistances.Count == 0)
            {
                loadAllPossibleJobAssignments();
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

            updateJobAssignmentDistances();
            updateMarkerVisibility();
            updateOverlays();
        }

        private void updateMarkerVisibility()
        {
            foreach (JobAssignment jobAssignment in jobAssignmentMarkers.Keys.ToArray())
            {
                bool isVisible = QuestingBotsPluginConfig.ShowQuestInfoOverlays.Value;
                isVisible &= jobAssignmentDistances[jobAssignment] <= QuestingBotsPluginConfig.QuestOverlayMaxDistance.Value;
                isVisible &= QuestingBotsPluginConfig.ShowQuestInfoForSpawnSearchQuests.Value || !jobAssignment.IsSpawnSearchQuest;

                jobAssignmentMarkers[jobAssignment].SetActive(isVisible);
            }
        }

        private void updateOverlays()
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

        private void updateJobAssignmentDistances()
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

            LoggingController.LogWarning("Loading all possible job assignments...");

            IEnumerable<JobAssignment> jobAssignments = BotJobAssignmentFactory.CreateAllPossibleJobAssignments();

            LoggingController.LogWarning("Loading all possible job assignments...creating objects...");

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
                QuestOverlayData overlayData = new QuestOverlayData(overlayPosition, questText);

                jobAssignmentDistances.Add(jobAssignment, float.PositiveInfinity);
                jobAssignmentMarkers.Add(jobAssignment, DebugHelpers.CreateSphere(stepPosition.Value, markerRadius * 2, Color.red));
                jobAssignmentInfo.Add(jobAssignment, overlayData);

                lastPosition = stepPosition.Value;
            }

            LoggingController.LogWarning("Loading all possible job assignments...done (Created " + jobAssignmentDistances.Count + " markers).");
        }

        internal class QuestOverlayData
        {
            public ActorDataStruct Data { get; set; }
            public GUIContent GuiContent { get; set; }
            public Rect GuiRect { get; set; }
            public Vector3 Position { get; set; }
            public string StaticText { get; set; }

            public QuestOverlayData()
            {
                GuiContent = new GUIContent();
                GuiRect = new Rect();
            }

            public QuestOverlayData(Vector3 _position) : this()
            {
                Position = _position;
            }

            public QuestOverlayData(Vector3 _position, string _staticText) : this(_position)
            {
                StaticText = _staticText;
            }
        }
    }
}
