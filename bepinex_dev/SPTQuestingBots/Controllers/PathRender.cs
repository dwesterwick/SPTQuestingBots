using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
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
    }
}
