using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class NavMeshHelpers
    {
        public static IEnumerable<Vector3> GetNavMeshTestPoints(this BoxCollider collider, float radius, float densityFactor)
        {
            // The Bounds for exfiltration colliders are junk in EFT, so we need to regenerate them here
            Bounds colliderBounds = new Bounds(collider.transform.position, collider.size);

            IEnumerable<Vector3> colliderTestPoints = GetNavMeshTestPoints(colliderBounds, radius, densityFactor);
            return colliderTestPoints;
        }

        public static IEnumerable<Vector3> GetNavMeshTestPoints(this Bounds bounds, float radius, float densityFactor)
        {
            float minExtent = Math.Min(Math.Min(bounds.size.x, bounds.size.x), bounds.size.x) / 2;
            if (minExtent < radius)
            {
                LoggingController.LogInfo("Radius " + radius + " is larger than min bounds extent " + minExtent + " of size " + bounds.size);

                return Enumerable.Empty<Vector3>();
            }

            // Determine the number of points to place on each axis
            int widthCount = (int)Math.Max(1, Math.Ceiling((bounds.size.x - (radius * 2)) * densityFactor / (2 * radius)));
            int lengthCount = (int)Math.Max(1, Math.Ceiling((bounds.size.z - (radius * 2)) * densityFactor / (2 * radius)));
            int heightCount = (int)Math.Max(1, Math.Ceiling((bounds.size.y - (radius * 2)) * densityFactor / (2 * radius)));

            // Determine the spacing of the points on each axis
            float widthSpacing = Math.Max(0, (bounds.size.x - (radius * 2)) / widthCount);
            float lengthSpacing = Math.Max(0, (bounds.size.z - (radius * 2)) / lengthCount);
            float heightSpacing = Math.Max(0, (bounds.size.y - (radius * 2)) / heightCount);

            // Create a 3D mesh of points within the bounds
            List<Vector3> testPoints = new List<Vector3>();
            Vector3 origin = new Vector3(bounds.min.x + radius, bounds.min.y + radius, bounds.min.z + radius);
            for (int x = 0; x <= widthCount; x++)
            {
                for (int y = 0; y <= heightCount; y++)
                {
                    for (int z = 0; z <= lengthCount; z++)
                    {
                        testPoints.Add(new Vector3(origin.x + (widthSpacing * x), origin.y + (heightSpacing * y), origin.z + (lengthSpacing * z)));
                    }
                }
            }

            return testPoints;
        }

        public static IList<Vector3> FindPointsOnNavMesh(this IEnumerable<Vector3> colliderTestPoints, float searchRadius)
        {
            LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();

            // For each point in the 3D mesh, try to find a point on the NavMesh within a certain radius
            List<Vector3> navMeshPoints = new List<Vector3>();
            foreach (Vector3 testPoint in colliderTestPoints)
            {
                Vector3? navMeshPoint = locationData.FindNearestNavMeshPosition(testPoint, searchRadius);
                if (navMeshPoint == null)
                {
                    continue;
                }

                // Do not allow duplicate point to be added, which is possible depending on the mesh density
                if (navMeshPoints.Any(x => x == navMeshPoint))
                {
                    continue;
                }

                navMeshPoints.Add(navMeshPoint.Value);
            }

            if (!navMeshPoints.Any())
            {
                LoggingController.LogWarning("Could not find any NavMesh points from " + colliderTestPoints.Count() + " test points using radius " + searchRadius + "m");
                LoggingController.LogWarning("Test points: " + string.Join(",", colliderTestPoints));
            }

            return navMeshPoints;
        }
    }
}
