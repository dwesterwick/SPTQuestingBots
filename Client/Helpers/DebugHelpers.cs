using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Helpers
{
    public static class DebugHelpers
    {
        public static bool IsSameAs(this ClassVector3 v1, ClassVector3 v2) => (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z);

        public static IEnumerable<Vector3> ApplyOffset(this IEnumerable<Vector3> points, Vector3 offset) => points.SelectNonAlloc(point => point + offset);

        public static Vector3 IncreaseVector3ToMinSize(Vector3 vector, float minSize)
        {
            return new Vector3((float)Math.Max(minSize, vector.x), (float)Math.Max(minSize, vector.y), (float)Math.Max(minSize, vector.z));
        }

        public static Vector3[] GetSpherePoints(Vector3 centerPoint, float radius, float pointCount)
        {
            return GetEllipsoidPoints(centerPoint, new Vector3(radius, radius, radius), pointCount);
        }

        public static Vector3[] GetEllipsoidPoints(Vector3 centerPoint, Vector3 radii, float pointCount)
        {
            List<Vector3> points = new List<Vector3>();

            // Draw a complete ellipse in the XY plane
            float theta_increment = (float)Math.PI * 2 / pointCount;
            for (float theta = 0; theta < 2 * Math.PI; theta += theta_increment)
            {
                float x = radii.x * (float)Math.Cos(theta);
                float y = radii.y * (float)Math.Sin(theta);

                points.Add(new Vector3(centerPoint.x + x, centerPoint.y + y, centerPoint.z));
            }
            points.Add(new Vector3(centerPoint.x + radii.x, centerPoint.y, centerPoint.z));

            // Draw a second ellipse in the XZ plane
            for (float theta = 0; theta < 2 * Math.PI; theta += theta_increment)
            {
                float x = radii.x * (float)Math.Cos(theta);
                float z = radii.z * (float)Math.Sin(theta);

                points.Add(new Vector3(centerPoint.x + x, centerPoint.y, centerPoint.z + z));
            }
            points.Add(new Vector3(centerPoint.x + radii.x, centerPoint.y, centerPoint.z));

            return points.ToArray();
        }

        public static Vector3[] GetBoundingBoxPoints(Bounds bounds)
        {
            return new Vector3[]
            {
                bounds.min,

                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),

                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),

                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),

                bounds.min,

                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                bounds.max,
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z)
            };
        }

        public static void DrawPositionOutline(Vector3 position, Color color)
        {
            DrawPositionOutline(position, color, 0.5f);
        }

        public static void DrawPositionOutline(Vector3 position, Color color, float radius)
        {
            string pathName = "Postion_" + "_" + DateTime.Now.ToFileTime();

            Vector3[] positionOutlinePoints = GetSpherePoints(position, radius, 10);
            Models.Pathing.PathVisualizationData positionOutline = new Models.Pathing.PathVisualizationData(pathName, positionOutlinePoints, color);
            Singleton<GameWorld>.Instance.GetComponent<PathRenderer>().AddOrUpdatePath(positionOutline);
        }

        public static GUIStyle CreateGuiStyleBotOverlays()
        {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.alignment = TextAnchor.MiddleLeft;
            guiStyle.fontSize = QuestingBotsPluginConfig.QuestOverlayFontSize.Value;
            guiStyle.margin = new RectOffset(3, 3, 3, 3);
            guiStyle.richText = true;

            return guiStyle;
        }

        public static GUIStyle CreateGuiStyleDebugText()
        {
            GUIStyle guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.alignment = TextAnchor.MiddleLeft;
            guiStyle.fontSize = QuestingBotsPluginConfig.QuestOverlayFontSize.Value;
            guiStyle.margin = new RectOffset(3, 3, 3, 3);
            guiStyle.richText = true;

            return guiStyle;
        }

        public static GameObject CreateSphere(Vector3 position, float size, Color color)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = color;
            sphere.GetComponent<Collider>().enabled = false;
            sphere.transform.position = position;
            sphere.transform.localScale = new Vector3(size, size, size);

            return sphere;
        }

        public static Color GetDebugColor(this NavMeshPathStatus status)
        {
            switch (status)
            {
                case NavMeshPathStatus.PathComplete: return Color.green;
                case NavMeshPathStatus.PathPartial: return Color.yellow;
                default: return Color.red;
            }
        }

        public static Color GetDebugColor(this EBotState botState)
        {
            switch (botState)
            {
                case EBotState.Active: return Color.green;
                case EBotState.PreActive: return Color.yellow;
                case EBotState.ActiveFail: return Color.red;
                default: return Color.yellow;
            }
        }

        public static Color GetDebugColor(this IBotDebugInfoSource debugInfoSource)
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return Color.white;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(debugInfoSource.NetId);
            if ((botPlayer == null) || !botPlayer.HealthController.IsAlive)
            {
                return Color.white;
            }

            Player? myPlayer = Singleton<PlayerData>.Instance.GetMyPlayer();
            if (myPlayer == null)
            {
                return Color.white;
            }

            if (!debugInfoSource.HasCurrentData)
            {
                return Color.white;
            }

            if (debugInfoSource.EnemyPlayerIds.AnyNonAlloc(id => id == myPlayer.Id))
            {
                return Color.red;
            }

            return Color.green;
        }

        public static bool ShouldShowPathStatus(this EBotQuestingDecision decision)
        {
            switch (decision)
            {
                case EBotQuestingDecision.Quest:
                case EBotQuestingDecision.Regroup:
                case EBotQuestingDecision.FollowBoss:
                case EBotQuestingDecision.HelpBoss:
                    return true;
            }

            return false;
        }

        public static bool CanDisplayDebugData(this IBotDebugInfoSource debugInfoSource, BepInEx.Configuration.ConfigEntry<QuestingBotType> config)
        {
            if (!Singleton<PlayerData>.Instantiated)
            {
                return false;
            }

            Player? botPlayer = Singleton<PlayerData>.Instance.GetPlayer(debugInfoSource.NetId);
            if (botPlayer == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot get position for bot with NetId=" + debugInfoSource.NetId);
                return false;
            }

            // Check if filter not empty (no filter), and bot is not included in the filter
            string botName = Singleton<PlayerData>.Instance.GetBotNameText(botPlayer);
            if (!DebugData.BotFilter.IsNullOrEmpty() && !DebugData.BotFilter.Contains(botName))
            {
                return false;
            }

            if (debugInfoSource.HasQuestingData && debugInfoSource.AllowedToQuest)
            {
                // Check if overlays are enabled for questing bosses (leaders)
                if (config.Value.HasFlag(QuestingBotType.QuestingLeaders) && !debugInfoSource.HasBoss)
                {
                    return true;
                }

                // Check if overlays are enabled for questing followers
                if (config.Value.HasFlag(QuestingBotType.QuestingFollowers) && debugInfoSource.HasBoss)
                {
                    return true;
                }
            }
            else
            {
                // Check if overlays are enabled for bots that are not questing
                if (config.Value.HasFlag(QuestingBotType.NonQuestingBots))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
