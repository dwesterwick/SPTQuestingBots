using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Controllers
{
    public class PathRender : MonoBehaviour
    {
        private static Dictionary<string, Models.PathVisualizationData> paths = new Dictionary<string, Models.PathVisualizationData>();
        private static object pathDictLock = new object();

        private void OnDisable()
        {
            Clear();
        }

        private void LateUpdate()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                return;
            }

            // Update each registered path
            lock (pathDictLock)
            {
                foreach (string pathName in paths.Keys)
                {
                    paths[pathName].Update();
                }
            }
        }

        public static void Clear()
        {
            // Prevent registered paths from being drawn again
            lock (pathDictLock)
            {
                foreach (string pathName in paths.Keys)
                {
                    paths[pathName].Clear();
                }

                paths.Clear();
            }
        }

        public static bool AddOrUpdatePath(Models.PathVisualizationData data, bool autoIncrementPathName = true)
        {
            if (data == null)
            {
                return false;
            }

            lock (pathDictLock)
            {
                while (autoIncrementPathName && paths.ContainsKey(data.PathName) && data.PathName.Length < 253)
                {
                    data.ChangeName(data.PathName + "_2");
                }

                if (paths.ContainsKey(data.PathName))
                {
                    // Need to erase the existing path before replacing it
                    //paths[data.PathName].Erase();
                    paths[data.PathName].Replace(data);
                }
                else
                {
                    paths.Add(data.PathName, data);
                }

                // Draw the new or updated path
                paths[data.PathName].Update();
            }

            return true;
        }

        public static bool RemovePath(string pathName)
        {
            lock (pathDictLock)
            {
                if (paths.ContainsKey(pathName))
                {
                    // Prevent the path from being drawn again
                    paths[pathName].Clear();

                    paths.Remove(pathName);
                    return true;
                }
            }

            return false;
        }

        public static bool RemovePath(Models.PathVisualizationData data)
        {
            if (data == null)
            {
                LoggingController.LogInfo("Path data is null");
                return false;
            }

            // In case the path isn't registered, erase it anyway
            if (!RemovePath(data.PathName))
            {
                LoggingController.LogInfo("Path " + data.PathName + " not found");
                data.Clear();
            }

            return true;
        }

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

        public static void outlinePosition(Vector3 position, Color color)
        {
            outlinePosition(position, color, 0.5f);
        }

        public static void outlinePosition(Vector3 position, Color color, float radius)
        {
            string pathName = "Postion_" + "_" + DateTime.Now.ToFileTime();

            Vector3[] positionOutlinePoints = PathRender.GetSpherePoints(position, radius, 10);
            PathVisualizationData positionOutline = new PathVisualizationData(pathName, positionOutlinePoints, color);
            PathRender.AddOrUpdatePath(positionOutline);
        }
    }
}
